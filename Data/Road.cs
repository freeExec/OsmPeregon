using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeExec.Tools;

namespace OsmPeregon.Data
{
    public class MatchMilestone
    {
        public float OriginalDistance;
        public float RealDistance;

        public MatchMilestone(float orig, float real)
        {
            OriginalDistance = orig;
            RealDistance = real;
        }
    }

    public class Road
    {
        private static StringBuilder sbGeojson = new StringBuilder(4096);


        private readonly List<Way> ways;
        //private List<bool> reversed;
        private List<Way> chainForward;

        public readonly long Id;
        public readonly string Ref;
        public readonly string Name;

        public IReadOnlyList<Way> Ways => ways;

        public Road(long id, string @ref, string name, IEnumerable<Way> ways)
        {
            Id = id;
            Ref = @ref;
            Name = name;
            this.ways = new List<Way>(ways);
            //this.reversed = new List<bool>(this.ways.Count);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Ref))
                return Name;
            else
                return $"{Name} ({Ref})";
        }

        public int ReorderingWays()
        {
            var t = ways
                .Select(w => Tuple.Create(w.Edges.First().NodeStart, w))
                .Concat(ways.Select(w => Tuple.Create(w.Edges.Last().NodeEnd, w)));
            var mapEdge = (Lookup<long, Way>)t.ToLookup(s => s.Item1, s => s.Item2);

            chainForward = new List<Way>();

            IEnumerable<Way> candidatList = mapEdge.Where(l => l.Count() == 1).First(l => l.Count(w => !w.IsBackward) > 0);
            Way lastWay = default;
            Way candidatWay = default;

            do
            {
                candidatWay = candidatList.FirstOrDefault(w => w.OrderStatus != OrderStatus.Reserve && !w.IsBackward);
                if (candidatWay == null)
                {

                }
                if (candidatWay != null)
                {
                    chainForward.Add(candidatWay);
                    candidatWay.OrderStatus = OrderStatus.Reserve;

                    if (lastWay != null && lastWay.LastNode != candidatWay.FirstNode)
                        candidatWay.ReverseDirection();

                    lastWay = candidatWay;
                    var last = candidatWay.LastNode;
                    candidatList = mapEdge[last];
                }

                if (lastWay.Id == 226280382L)
                {
                    int gg = 99;
                }

            } while (candidatWay != null);

            var nouse = mapEdge.Where(l => l.Any(w => w.OrderStatus != OrderStatus.Reserve)).ToList();
            return chainForward.Count;
        }

        public float GetShiftMilestones(Dictionary<long, float> osmMilestones)
        {

            sbGeojson.Length = 0;
            sbGeojson.AppendLine("{\"type\": \"FeatureCollection\",\"features\": [");

            float length = 0f;
            var deltas = new List<MatchMilestone>();
            float lastMile = 0;
            float lastLength = 0;
            foreach (var way in chainForward)
            {
                foreach (var edge in (way.IsReverse ? way.Edges.Reverse() : way.Edges))
                {
                    float mile = 0;

                    long lastNode = way.IsReverse ? edge.NodeStart : edge.NodeEnd;

                    length += edge.Length;
                    if (osmMilestones.TryGetValue(lastNode, out mile) && mile > 0)
                    {
                        //int[] pp = way.IsReverse ? edge.Start : edge.End;
                        //sbGeojson.Append($"{{\"type\":\"Feature\",\"properties\":{{ \"label\":\"{mile}\"}},");
                        //sbGeojson.Append($"\"geometry\":{{\"type\":\"Point\",\"coordinates\":[{pp[0] * H3.GeoTools.FACTOR:F6},{pp[1] * H3.GeoTools.FACTOR:F6}]}}}}");
                        //sbGeojson.AppendLine(",");

                        //Console.WriteLine($"{mile:000} \t {length:F2} \t {mile - length:F2} \t {(length - lastLength)/(mile - lastMile):F2}");
                        deltas.Add(new MatchMilestone(mile, length));

                        lastMile = mile;
                    }
                    lastLength = length;
                }
            }

            //sbGeojson.Length -= Environment.NewLine.Length + 1;
            //sbGeojson.AppendLine("]}");
            //var geoJson = sbGeojson.ToString();

            //File.WriteAllText("orig.geojson", geoJson);

            var avg = deltas.Average(mm => mm.OriginalDistance - mm.RealDistance);
            var std = deltas.Std(mm => mm.OriginalDistance - mm.RealDistance);
            return avg;
        }

        public string GenerateGeoJsonFromLinearInterpolate(float startShift)
        {
            sbGeojson.Length = 0;
            sbGeojson.AppendLine("{\"type\": \"FeatureCollection\",\"features\": [");
            float lengthTotal = startShift;
            float nextMilestone = MathF.Floor(startShift + 1);
            foreach (var way in chainForward)
            {
                foreach (var edge in (way.IsReverse ? way.Edges.Reverse() : way.Edges))
                {
                    float length = edge.Length;

                    while (lengthTotal + length > nextMilestone)
                    {
                        var shortLength = nextMilestone - lengthTotal;
                        float lineFactor = shortLength / length;

                        int[] pos = edge.InterpolatePosition(way.IsReverse ? 1 - lineFactor : lineFactor);
                        int posLon = pos[0];
                        int posLat = pos[1];

                        sbGeojson.Append($"{{\"type\":\"Feature\",\"properties\":{{ \"label\":\"{nextMilestone}\"}},");
                        sbGeojson.Append($"\"geometry\":{{\"type\":\"Point\",\"coordinates\":[{posLon * H3.GeoTools.FACTOR:F6},{posLat * H3.GeoTools.FACTOR:F6}]}}}}");
                        sbGeojson.AppendLine(",");

                        nextMilestone += 1;
                    }

                    lengthTotal += length;
                }
            }

            sbGeojson.Length -= Environment.NewLine.Length + 1;
            sbGeojson.AppendLine("]}");

            return sbGeojson.ToString();
        }

        public string GenerateGeoJsonFromBaseMilestone(Dictionary<long, float> osmMilestones)
        {

            sbGeojson.Length = 0;
            sbGeojson.AppendLine("{\"type\": \"FeatureCollection\",\"features\": [");

            Action<float, float, Edge, bool> PutMilestoneInJson = (float milestone, float currentLength, Edge edge, bool isReverse) =>
            {
                float length = edge.Length;
                var shortLength = milestone - currentLength;
                float lineFactor = shortLength / length;
                int[] pos = edge.InterpolatePosition(isReverse ? 1 - lineFactor : lineFactor);

                sbGeojson.Append($"{{\"type\":\"Feature\",\"properties\":{{ \"label\":\"{milestone}\"}},");
                sbGeojson.Append($"\"geometry\":{{\"type\":\"Point\",\"coordinates\":[{pos[0] * H3.GeoTools.FACTOR:F6},{pos[1] * H3.GeoTools.FACTOR:F6}]}}}}");
                sbGeojson.AppendLine(",");
            };

            float lengthTotal = 0;
            float nextMilestone = lengthTotal + 1;

            foreach (var way in chainForward)
            {
                foreach (var edge in (way.IsReverse ? way.Edges.Reverse() : way.Edges))
                {
                    float length = edge.Length;

                    if (lengthTotal + length >= 162)
                    {
                        int gg = 99;
                    }

                    while (lengthTotal + length > nextMilestone)
                    {
                        PutMilestoneInJson(nextMilestone, lengthTotal, edge, way.IsReverse);

                        nextMilestone += 1;
                    }

                    lengthTotal += length;

                    long lastNode = way.IsReverse ? edge.NodeStart : edge.NodeEnd;
                    if (osmMilestones.TryGetValue(lastNode, out float mile) && mile > 0)
                    {
                        if (mile == 174)
                        {
                            int gg = 99;
                        }
                        if (lengthTotal < mile)
                        {
                            PutMilestoneInJson(mile, way.IsReverse ? mile - length : mile, edge, way.IsReverse);
                        }

                        lengthTotal = mile;
                        nextMilestone = lengthTotal + 1;
                    }
                }
            }

            sbGeojson.Length -= Environment.NewLine.Length + 1;
            sbGeojson.AppendLine("]}");
            return sbGeojson.ToString();
        }
    }
}
