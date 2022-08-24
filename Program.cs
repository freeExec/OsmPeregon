//#define LOCAL

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
#else
        private const int OSM_ROAD_COUNT          =   37000;
        private const int OSM_WAY_COUNT           =  322000;
        private const int OSM_EDGE_COUNT          = 4120000;
        private const int OSM_MILESTONE_COUNT     =   12000;
        private const int OSM_NEW_MILESTONE_COUNT =  150000;
#endif

        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            //var o5mSource = @"d:\frex\Test\OSM\RU_local\highway_road.o5m";
            var o5mSource = @"i:\MyWorkProg\Map_Gis\Styles\Highway\highway-local-RU.o5m";
            //var o5mSource = "relation-ural-ulyanovsk.o5m";
            //var o5mSource = "R-178.o5m";
            //var o5mSource = "test-road.o5m";
            var o5mReader = new O5mStreamReader(o5mSource);

            var mailstoneDictionary = new Dictionary<long, float>(OSM_MILESTONE_COUNT);

            foreach (var record in o5mReader.SectionNode)
            {
                if (record.Contains(OsmConstants.KEY_HIHGWAY, OsmConstants.TAG_MILESTONE))
                {
                    var distanceTagStr = record.GetTagValue(OsmConstants.KEY_DISTANCE);
                    if (string.IsNullOrEmpty(distanceTagStr))
                        distanceTagStr = record.GetTagValue(OsmConstants.KEY_PK);

                    if (!string.IsNullOrEmpty(distanceTagStr))
                    {
                        if (float.TryParse(distanceTagStr, out float distanceTag))
                        {
                            mailstoneDictionary.Add(record.Id, distanceTag);
                        }
                        else
                        {
                            //Console.WriteLine($"n{record.Id} - {distanceTagStr}");
                        }
                    }
                }
            }

            var roadDictionary = new Dictionary<long, Road>(OSM_ROAD_COUNT);
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

                    roadDictionary.Add(road.Id, road);
                }
            }

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
            }

            long lastNodeId = 0;
            foreach (var record in o5mReader.SectionNode)
            {
                if (edgeDictionary.TryGetValue(record.Id, out List<Edge> edges))
                {
                    var coord = new int[] { record.LonI, record.LatI };
                    foreach (var edge in edges)
                    {
                        if (record.Id == edge.NodeStart)
                            edge.Start = new GeomPoint(coord);
                        if (record.Id == edge.NodeEnd)
                            edge.End = new GeomPoint(coord);
                    }
                }
                lastNodeId = record.Id;
            }

            long globalNewNodeId = (long)(lastNodeId / 100000.0) * 100000;
            var newNodesMilestone = new List<FormatsOsm.WriteModel.Node>(OSM_NEW_MILESTONE_COUNT);

            foreach (var road in roadDictionary.Values)
            {
                if (!road.IsCorrect)
                {
                    Console.WriteLine($"Skip road: {road}");
                    continue;
                }

                int chainCount = road.CreateChainWays();
                if (chainCount > 0)
                {
                    float shift = road.GetShiftMilestones(mailstoneDictionary);
                    bool hasBaseMalestone = !float.IsNaN(shift);

                    if (float.IsNaN(shift))
                    {
                        shift = 0;
                    }

                    var milestonesInter = road.GetMilestonesLinearInterpolate(shift);
                    //string geojsonInterpolation = GeojsonGenerator.FromMilestones(milestonesInter);
                    //File.WriteAllText("interpolation.geojson", geojsonInterpolation);

                    newNodesMilestone.AddRange(milestonesInter.Select(m => ToOsmNodeInterpolate(m, globalNewNodeId++)));

                    if (hasBaseMalestone)
                    {
                        var milestonesBase = road.GetMilestonesBaseOriginal(mailstoneDictionary);
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
                    }
                }
            }
            SaveOsmDumpWithGeneratedMilestones(newNodesMilestone);
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

        //private static void SaveOsmDumpWithGeneratedMilestones(O5mStreamReader reader, IEnumerable<FormatsOsm.WriteModel.Node> nodes)
        //{
        //    using (var writer = new O5mStreamWriter("generated-milestones.o5m"))
        //    {
        //        var node = new FormatsOsm.WriteModel.Node(0, 0, 0);
        //        var way = new FormatsOsm.WriteModel.Way(0);
        //        var rel = new FormatsOsm.WriteModel.Relation(0);

        //        bool isNewMilestonesSaved = false;

        //        foreach (var record in reader)
        //        {
        //            switch (record.Type)
        //            {
        //                case O5mHeaderSign.TimeStamp:
        //                    writer.WriteFileTimestamp(record.Timestamp);
        //                    break;

        //                case O5mHeaderSign.Bbox:
        //                    int x1 = (int)record.GetRef(0);
        //                    int y1 = (int)record.GetRef(1);
        //                    int x2 = (int)record.GetRef(2);
        //                    int y2 = (int)record.GetRef(3);

        //                    writer.WriteBbox(x1, y1, x2, y2);
        //                    break;

        //                case O5mHeaderSign.Node:
        //                    node.Fill(record);
        //                    writer.WriteNode(node);
        //                    break;
        //                case O5mHeaderSign.Way:
        //                    if (!isNewMilestonesSaved)
        //                    {
        //                        foreach (var milestoneNode in nodes)
        //                        {
        //                            writer.WriteNode(milestoneNode);
        //                        }
        //                        isNewMilestonesSaved = true;
        //                    }
        //                    way.Fill(record);
        //                    writer.WriteWay(way);
        //                    break;
        //                case O5mHeaderSign.Relation:
        //                    rel.Fill(record);
        //                    writer.WriteRelation(rel);
        //                    break;
        //            }
        //        }
        //    }
        //}
    }
}
