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
        private float statStartMilestone;

        private float outlierMin;
        private float outlierMax;

        private List<MilestoneMatch> errorsMilestonesDistance;

        public readonly long Id;
        public readonly string Ref;
        public readonly string Name;

        public IReadOnlyList<Way> Ways => ways;
        public bool IsCorrect => ways.All(w => w.IsCorrect);

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
            } while (candidatWay != null);

            var nouse = mapEdge.Where(l => l.Any(w => w.OrderStatus != OrderStatus.Reserve)).ToList();
            return chainForward.Count;
        }

        public bool CalculationStatisticsAndMatchMilestones(Dictionary<long, float> osmMilestones, bool forceExit = false)
        {
            float length = 0f;
            errorsMilestonesDistance = new List<MilestoneMatch>();
            float lastMile = 0;
            float lastLength = 0;

            int nextLess = 0;
            int nextMore = 0;
            foreach (var way in chainForward)
            {
                foreach (var edge in (way.Edges))
                {
                    float mile = 0;

                    long lastNode = way.IsReverse ? edge.NodeStart : edge.NodeEnd;

                    length += edge.Length;
                    if (osmMilestones.TryGetValue(lastNode, out mile) && mile > 0)
                    {
                        if (mile > lastMile)
                            nextMore++;
                        else
                            nextLess++;

                        errorsMilestonesDistance.Add(new MilestoneMatch(mile, length));
                        lastMile = mile;
                    }
                    lastLength = length;
                }
            }

            if (nextLess > nextMore)
            {
                if (forceExit)
                    throw new NotSupportedException();

                chainForward.Reverse();
                chainForward.ForEach(c => c.ReverseDirection());

                return CalculationStatisticsAndMatchMilestones(osmMilestones, true);
            }

            if (errorsMilestonesDistance.Count > 0)
            {

                statStartMilestone = errorsMilestonesDistance.Average(e => e.Error);

                (outlierMin, outlierMax) = Outlier.GetOutlierBoundary(errorsMilestonesDistance.Select(e => e.Error), true);
                foreach (var badMilestone in errorsMilestonesDistance.Where(e => e.Error <= outlierMin || e.Error >= outlierMax))
                    badMilestone.IsBad = true;

                statStartMilestone = errorsMilestonesDistance.Where(e => !e.IsBad).Average(e => e.Error);

                return true;
            }

            return false;
        }

        public List<MilestonePoint> GetMilestonesLinearInterpolate()
        {
            float lengthTotal = statStartMilestone;
            float nextMilestone = MathF.Floor(statStartMilestone + MILESTONE_STEP_KM);

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

        public List<MilestonePoint> GetMilestonesBaseOriginal(Dictionary<long, float> osmMilestones)
        {
            Func<float, float, Edge, bool, bool, MilestonePoint> GetMilestone = (float milestone, float currentLength, Edge edge, bool isReverse, bool isOriginal) =>
            {
                float length = edge.Length;
                var shortLength = milestone - currentLength;
                float lineFactor = shortLength / length;
                GeomPoint pos = edge.InterpolatePosition(isReverse ? 1 - lineFactor : lineFactor);
                return new MilestonePoint(milestone, pos, isOriginal);
            };

            float lengthTotal = 0;
            float nextMilestone = lengthTotal + MILESTONE_STEP_KM;

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

                        nextMilestone += MILESTONE_STEP_KM;
                    }

                    lengthTotal += length;

                    long lastNode = way.IsReverse ? edge.NodeStart : edge.NodeEnd;
                    if (osmMilestones.TryGetValue(lastNode, out float mile) && mile > 0)
                    {
                        milestonePoints.Add(new MilestonePoint(mile, way.IsReverse ? edge.Start : edge.End, true));

                        lengthTotal = mile;
                        nextMilestone = lengthTotal + MILESTONE_STEP_KM;
                    }
                }
            }

            return milestonePoints;
        }
    }
}
