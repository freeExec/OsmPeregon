//#define LOCAL
//#define DETAIL_ROAD_GEN_LOG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FormatsOsm;
using FreeExec.Geom;
using OsmPeregon.Data;

namespace OsmPeregon
{
    class Program
    {
#if LOCAL
        private const int OSM_ROAD_COUNT = 10;
        private const int OSM_WAY_COUNT = 200;
        private const int OSM_EDGE_COUNT = 1000;
        private const int OSM_MILESTONE_COUNT = 50;
        private const int OSM_NEW_MILESTONE_COUNT = 1500;
#else
        private const int OSM_ROAD_COUNT          =   37000;
        private const int OSM_WAY_COUNT           =  322000;
        private const int OSM_EDGE_COUNT          = 4120000;
        private const int OSM_MILESTONE_COUNT     =   17000;
        private const int OSM_NEW_MILESTONE_COUNT =  150000;
#endif

        private const double INTERVAL_DISPLAY_PROGRESS = 0.6;

        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            var name = Assembly.GetExecutingAssembly().GetName().Name;
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var about = $"{name} (c) freeExec 2022 v{version.Major}.{version.Minor}.{version.Build}";
            Console.WriteLine(about);

            var token = new ConsoleTokenizer(args);

            if (token.Files.Count < 1)
            {
                Console.WriteLine($"Usage: {name}.exe [options] highway.o5m");
                Console.WriteLine("\t-l");
                Console.WriteLine("\t\t<log file>");
                Console.WriteLine();

                return;
            }

            var o5mSource = token.Files[0];

            var statsInfo = new StatInfo();
            var o5mReader = new O5mStreamReader(o5mSource);

            int columnInfoPosForProgress = 0;

            Console.Write("Collecting road info ... ");
            var roads = new List<Road>(OSM_ROAD_COUNT);
            var wayDictionary = new Dictionary<long, Way>(OSM_WAY_COUNT);

            foreach (var record in o5mReader.SectionRelation)
            {
                if (record.Contains(OsmConstants.KEY_TYPE, OsmConstants.KEY_ROUTE)
                    && record.Contains(OsmConstants.KEY_ROUTE, OsmConstants.TAG_ROAD))
                {
                    var road = new Road(
                        record.Id,
                        record.GetTagValue(OsmConstants.KEY_REF),
                        record.GetTagValue(OsmConstants.KEY_NAME),
                        record.Members.Select(member =>
                        {
                            if (wayDictionary.TryGetValue(member.Id, out Way w))
                                return w;
                            return new Way(member.Id, member.Role);
                        })
                    );

                    foreach (var way in road.Ways)
                        wayDictionary[way.Id] = way;

                    roads.Add(road);
                }
            }
            Console.WriteLine("OK");

            statsInfo.RoadTotal = roads.Count;

            Console.Write("Collecting edges ... ");
            columnInfoPosForProgress = Console.CursorLeft;
            DateTime lastViewedProgressTime = DateTime.Now;

            var edgeDictionary = new Dictionary<long, List<Edge>>(OSM_EDGE_COUNT);

            foreach (var reader in o5mReader.SectionWay)
            {
                if (wayDictionary.TryGetValue(reader.Id, out Way way))
                {
                    way.AddEdges(reader.Refs.Pairwise().Select(pair => new Edge(pair.Previous, pair.Current)));
                    foreach (var edge in way.Edges)
                    {
                        Action<long, Edge> addNodeForEdge = (long nodeId, Edge edg) =>
                        {
                            List<Edge> nodeForEdges;
                            if (!edgeDictionary.TryGetValue(nodeId, out nodeForEdges))
                            {
                                nodeForEdges = new List<Edge>();
                                edgeDictionary.Add(nodeId, nodeForEdges);
                            }
                            nodeForEdges.Add(edg);
                        };

                        addNodeForEdge(edge.NodeStart, edge);
                        addNodeForEdge(edge.NodeEnd, edge);
                    }
                }

                if (!Console.IsOutputRedirected)
                {
                    if (DateTime.Now.Subtract(lastViewedProgressTime).TotalSeconds > INTERVAL_DISPLAY_PROGRESS)
                    {
                        Console.CursorLeft = columnInfoPosForProgress;
                        float progress = o5mReader.GetSectionProgress(O5mHeaderSign.Way);
                        Console.Write(progress.ToString("P2"));
                        lastViewedProgressTime = DateTime.Now;
                    }
                }
            }
            if (!Console.IsOutputRedirected)
                Console.CursorLeft = columnInfoPosForProgress;
            Console.WriteLine("OK          ");

            Console.Write("Collecting coordinates and milestones ... ");
            columnInfoPosForProgress = Console.CursorLeft;
            long lastNodeId = 0;
            var milestoneDictionary = new Dictionary<long, float>(OSM_MILESTONE_COUNT);
            lastViewedProgressTime = DateTime.Now;

            var badMilestones = new List<(long id, GeomPoint geom, string value)>(1000);

            foreach (var record in o5mReader.SectionNode)
            {
                if (record.Contains(OsmConstants.KEY_HIHGWAY, OsmConstants.TAG_MILESTONE))
                {
                    var distanceTagStr = record.GetTagValue(OsmConstants.KEY_DISTANCE);
                    if (string.IsNullOrEmpty(distanceTagStr))
                        distanceTagStr = record.GetTagValue(OsmConstants.KEY_PK);

                    if (!string.IsNullOrEmpty(distanceTagStr))
                    {
                        var distanceTagFilered = distanceTagStr.Replace("km", string.Empty).Replace("км", string.Empty).TrimEnd();
                        if (float.TryParse(distanceTagFilered, out float distanceTag))
                        {
                            milestoneDictionary.Add(record.Id, distanceTag);
                        }
                        else
                        {
                            //Console.WriteLine($"n{record.Id} - {distanceTagStr}");
                            badMilestones.Add(new (record.Id, new GeomPoint(record.LonI, record.LatI), distanceTagStr));
                        }
                    }
                }

                if (edgeDictionary.TryGetValue(record.Id, out List<Edge> edges))
                {
                    foreach (var edge in edges)
                    {
                        if (record.Id == edge.NodeStart)
                            edge.Start = new GeomPoint(record.LonI, record.LatI);
                        else if (record.Id == edge.NodeEnd)
                            edge.End = new GeomPoint(record.LonI, record.LatI);
                    }
                }
                lastNodeId = record.Id;

                if (!Console.IsOutputRedirected)
                {
                    if (DateTime.Now.Subtract(lastViewedProgressTime).TotalSeconds > INTERVAL_DISPLAY_PROGRESS)
                    {
                        Console.CursorLeft = columnInfoPosForProgress;
                        float progress = o5mReader.GetSectionProgress(O5mHeaderSign.Node);
                        Console.Write(progress.ToString("P2"));
                        lastViewedProgressTime = DateTime.Now;
                    }
                }
            }
            if (!Console.IsOutputRedirected)
                Console.CursorLeft = columnInfoPosForProgress;
            Console.WriteLine("OK          ");

            o5mReader.Close();

            statsInfo.Milestones = milestoneDictionary.Count;
            statsInfo.BadMilestones = badMilestones.Count;

            //var geojsonBadMelistones = GeojsonGenerator.FromNodeIds(badMilestones);
            //File.WriteAllText("bad-melistones-maproulette.geojson", geojsonBadMelistones);

            long globalNewNodeId = (long)(lastNodeId / 100000.0) * 100000;
            var newNodesMilestone = new List<FormatsOsm.WriteModel.Node>(OSM_NEW_MILESTONE_COUNT);

            foreach (var road in roads)
            {
                if (road.Ways.Count == 0)
                {
                    statsInfo.RoadEmpty++;
#if DETAIL_ROAD_GEN_LOG
                    Console.WriteLine($"Skip road (empty): {road}");
#endif
                    continue;
                }

                if (!road.IsCorrect)
                {
#if DETAIL_ROAD_GEN_LOG
                    Console.WriteLine($"Skip road (incorrect): {road}");
#endif
                    continue;
                }
                statsInfo.RoadCorrect++;

                (int chainCount, int noUse) = road.CreateChainWays();
                statsInfo.NoUseWays += noUse;
                if (chainCount > 0)
                {
                    float length = road.CalculationStatisticsAndMatchMilestones(milestoneDictionary);
                    statsInfo.TotalLength += length;
                    bool hasBaseMalestone = length > 0;
                    var milestonesInter = road.GetMilestonesLinearInterpolate();
                    //string geojsonInterpolation = GeojsonGenerator.FromMilestones(milestonesInter);
                    //File.WriteAllText("interpolation.geojson", geojsonInterpolation);

                    newNodesMilestone.AddRange(milestonesInter.Select(m => ToOsmNodeInterpolate(m, globalNewNodeId++)));

                    if (hasBaseMalestone)
                    {
                        var milestonesBase = road.GetMilestonesBaseOriginal(milestoneDictionary);
                        //string geojsonBaseMilestone = GeojsonGenerator.FromMilestones(milestonesBase);
                        //File.WriteAllText("interpolation-base-milestone.geojson", geojsonBaseMilestone);

                        //string geojsonOrigMilestone = GeojsonGenerator.FromMilestones(milestonesBase.Where(m => m.IsOriginal));
                        //File.WriteAllText("orig.geojson", geojsonOrigMilestone);

                        MilestonePoint prevMilestone = milestonesBase.First();
                        newNodesMilestone.AddRange(
                            milestonesBase
                                .GroupBy(m => m.Milestone)
                                .SelectMany(m =>
                                    m.Any(m => m.IsOriginal) ? m.Where(m => m.IsOriginal) : m
                                )
                                .Select(m =>
                                {
                                    var node = ToOsmNodeBase(m, prevMilestone, globalNewNodeId++);
                                    prevMilestone = m;
                                    return node;
                                })
                        );

                        statsInfo.RoadWithMilestones++;
                    }
#if DETAIL_ROAD_GEN_LOG
                    Console.WriteLine($"Generate C:{chainCount}, N:{noUse} I{(hasBaseMalestone ? "B" : "")}: {road}");
#endif
                }
#if DETAIL_ROAD_GEN_LOG
                else
                    Console.WriteLine($"Skip road (no chains) N:{noUse} : {road}");
#endif
            }
            SaveOsmDumpWithGeneratedMilestones(newNodesMilestone);

            PrintStats(statsInfo, token["l"]);
        }

        private static FormatsOsm.WriteModel.Node ToOsmNodeInterpolate(MilestonePoint milestone, long id)
        {
            var node = new FormatsOsm.WriteModel.Node(id, milestone.GeomPosition.LongitudeI, milestone.GeomPosition.LatitudeI);
            node.AddTag(OsmConstants.KEY_HIHGWAY, OsmConstants.TAG_MILESTONE);
            node.AddTag(OsmConstants.KEY_DISTANCE, milestone.Milestone.ToString());
            node.AddTag("generate", "interpolate");
            return node;
        }

        private static FormatsOsm.WriteModel.Node ToOsmNodeBase(MilestonePoint milestone, MilestonePoint prevMilestone, long id)
        {
            var node = new FormatsOsm.WriteModel.Node(id, milestone.GeomPosition.LongitudeI, milestone.GeomPosition.LatitudeI);
            node.AddTag(OsmConstants.KEY_HIHGWAY, OsmConstants.TAG_MILESTONE);
            node.AddTag(OsmConstants.KEY_DISTANCE, milestone.Milestone.ToString());
            node.AddTag("generate", "base");
            node.AddTag("error", (1 - (milestone.Milestone - prevMilestone.Milestone)).ToString("F3"));
            return node;
        }

        private static void SaveOsmDumpWithGeneratedMilestones(IEnumerable<FormatsOsm.WriteModel.Node> nodes)
        {
            using (var writer = new O5mStreamWriter("generated-milestones.o5m"))
            {
                writer.WriteFileTimestamp(O5mHelper.DateTime2Timestamp(DateTime.UtcNow));
                foreach (var milestoneNode in nodes)
                {
                    writer.WriteNode(milestoneNode);
                }
            }
        }

        private static void PrintStats(StatInfo stats, string logFilePath)
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine($"RoadTotal: {stats.RoadTotal}");
            sb.AppendLine($"RoadEmpty: {stats.RoadEmpty}");
            sb.AppendLine($"RoadCorrect: {stats.RoadCorrect}");
            sb.AppendLine($"RoadWithMilestones: {stats.RoadWithMilestones}");
            sb.AppendLine($"NoUseWays: {stats.NoUseWays}");
            sb.AppendLine($"Milestones: {stats.Milestones}");
            sb.AppendLine($"BadMilestones: {stats.BadMilestones}");
            sb.AppendLine($"TotalLength: {stats.TotalLength:F3}");

            var statsLog = sb.ToString();

            Console.WriteLine();
            Console.WriteLine(statsLog);
            
            if (!string.IsNullOrEmpty(logFilePath))
                File.WriteAllText(logFilePath, statsLog);
        }

        private class StatInfo
        {
            public int RoadTotal;
            public int RoadEmpty;
            public int RoadCorrect;
            public int RoadWithMilestones;

            public int NoUseWays;
            public int Milestones;
            public int BadMilestones;

            public double TotalLength;
        }
    }
}
