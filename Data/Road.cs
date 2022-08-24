using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeExec.Geom;
using FreeExec.Tools;

namespace OsmPeregon.Data
{
    public class Road
    {
        private const float MILESTONE_STEP_KM = 1;

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

        public int CreateChainWays()
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
            float length = 0f;
            //var deltas = new List<MatchMilestone>();
            var deltas = new List<float>();
            float lastMile = 0;
            float lastLength = 0;
            foreach (var way in chainForward)
            {
                foreach (var edge in (way.Edges))
                {
                    float mile = 0;

                    long lastNode = way.IsReverse ? edge.NodeStart : edge.NodeEnd;

                    length += edge.Length;
                    if (osmMilestones.TryGetValue(lastNode, out mile) && mile > 0)
                    {
                        //Console.WriteLine($"{mile:000} \t {length:F2} \t {mile - length:F2} \t {(length - lastLength)/(mile - lastMile):F2}");
                        //deltas.Add(new MatchMilestone(mile, length));
                        deltas.Add(mile - length);

                        lastMile = mile;
                    }
                    lastLength = length;
                }
            }

            //var avg = deltas.Average(mm => mm.OriginalDistance - mm.RealDistance);
            //var std = deltas.Std(mm => mm.OriginalDistance - mm.RealDistance);

            if (deltas.Count > 0)
            {
                var avg = deltas.Average();
                var std = deltas.Std();
                if (std > 1)
                {
                    int gg = 99;
                    chainForward.Reverse();
                    chainForward.ForEach(c => c.ReverseDirection());

                    return GetShiftMilestones(osmMilestones);
                }
                return avg;
            }

            return float.NaN;
        }

        public List<MilestonePoint> GetMilestonesLinearInterpolate(float startShift)
        {
            float lengthTotal = startShift;
            float nextMilestone = MathF.Floor(startShift + MILESTONE_STEP_KM);

            var firstWay = chainForward.First();
            var firstEdge = firstWay.Edges.First();
            var firstPoint = firstEdge.InterpolatePosition(firstWay.IsReverse ? 1 : 0);
            var milestonePoints = new List<MilestonePoint>(chainForward.Count * 20)
            {
                new MilestonePoint(lengthTotal, firstPoint, false)
            };

            foreach (var way in chainForward)
            {
                foreach (var edge in way.Edges)
                {
                    float length = edge.Length;

                    while (lengthTotal + length > nextMilestone)
                    {
                        var shortLength = nextMilestone - lengthTotal;
                        float lineFactor = shortLength / length;
                        GeomPoint pos = edge.InterpolatePosition(way.IsReverse ? 1 - lineFactor : lineFactor);
                        milestonePoints.Add(new MilestonePoint(nextMilestone, pos, false));

                        nextMilestone += 1;
                    }

                    lengthTotal += length;
                }
            }

            return milestonePoints;
        }

        public List<MilestonePointToInsertOsm> GetMilestonesBaseOriginal(Dictionary<long, float> osmMilestones)
        {
            Func<float, float, Edge, bool, bool, MilestonePointToInsertOsm> GetMilestone = (float milestone, float currentLength, Edge edge, bool isReverse, bool isOriginal) =>
            {
                float length = edge.Length;
                var shortLength = milestone - currentLength;
                float lineFactor = shortLength / length;
                GeomPoint pos = edge.InterpolatePosition(isReverse ? 1 - lineFactor : lineFactor);
                return new MilestonePointToInsertOsm(milestone, pos, isOriginal);
            };

            float lengthTotal = 0;
            float nextMilestone = lengthTotal + MILESTONE_STEP_KM;

            var firstWay = chainForward.First();
            var firstEdge = firstWay.Edges.First();
            var firstPoint = firstEdge.InterpolatePosition(firstWay.IsReverse ? 1 : 0);
            var milestonePoints = new List<MilestonePointToInsertOsm>(chainForward.Count * 20)
            {
                new MilestonePointToInsertOsm(lengthTotal, firstPoint, false)
                {
                    Way = firstWay,
                    Edge = firstEdge
                }
            };

            foreach (var way in chainForward)
            {
                foreach (var edge in way.Edges)
                {
                    float length = edge.Length;

                    while (lengthTotal + length > nextMilestone)
                    {
                        var shortLength = nextMilestone - lengthTotal;
                        float lineFactor = shortLength / length;
                        GeomPoint pos = edge.InterpolatePosition(way.IsReverse ? 1 - lineFactor : lineFactor);

                        milestonePoints.Add(new MilestonePointToInsertOsm(nextMilestone, pos, false)
                        {
                            Way = way,
                            Edge = edge
                        });

                        nextMilestone += MILESTONE_STEP_KM;
                    }

                    lengthTotal += length;

                    long lastNode = way.IsReverse ? edge.NodeStart : edge.NodeEnd;
                    if (osmMilestones.TryGetValue(lastNode, out float mile) && mile > 0)
                    {
                        milestonePoints.Add(new MilestonePointToInsertOsm(mile, way.IsReverse ? edge.Start : edge.End, true));

                        lengthTotal = mile;
                        nextMilestone = lengthTotal + MILESTONE_STEP_KM;
                    }
                }
            }

            return milestonePoints;
        }

        //private class MatchMilestone
        //{
        //    public float OriginalDistance;
        //    public float RealDistance;

        //    public MatchMilestone(float orig, float real)
        //    {
        //        OriginalDistance = orig;
        //        RealDistance = real;
        //    }
        //}
    }
}
