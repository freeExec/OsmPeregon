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

        private enum MatchMilestonesErrorCode
        {
            OK = 0,
            NoMilestones,
            NextEntryPoint,
            DublicateMilestones,
            NoVariants,
        }

        private readonly List<Way> ways;
        //private List<bool> reversed;
        private List<Way> chainForward;
        private float statStartMilestone;

        private Dictionary<long, MilestoneMatch> errorsMilestonesDistance;

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

        public (int use, int noUse, float length, bool hasMilestone) CreateChainWays(Dictionary<long, float> osmMilestones)
        {
            var t = ways
                .Select(w => Tuple.Create(w.Edges.First().NodeStart, w))
                .Concat(ways.Select(w => Tuple.Create(w.Edges.Last().NodeEnd, w)));
            var mapEdge = (Lookup<long, Way>)t.ToLookup(s => s.Item1, s => s.Item2);

            int entryPointsCount = mapEdge.Count(l => l.Count() == 1 && l.Any(w => !w.IsNotEnter));
            bool isRoundabout = entryPointsCount == 0;

            var tt = mapEdge.Where(l => l.Count() == 1).ToList();

            chainForward = new List<Way>();
            int skipEntryPoint = 0;

            float length = 0;
            MatchMilestonesErrorCode code;
            bool hasMilestone = true;

            for (int entryPointIndex = 0; entryPointIndex < entryPointsCount || isRoundabout; entryPointIndex++)
            {
                chainForward.Clear();
                ways.ForEach(w => w.OrderStatus = OrderStatus.None);

                IGrouping<long, Way> candidatList;
                if (isRoundabout)
                {
                    candidatList = mapEdge.FirstOrDefault(l => l.Any(w => !w.IsNotEnter));
                    isRoundabout = false;
                }
                else
                {
                    candidatList = mapEdge.Where(l => l.Count() == 1).Skip(skipEntryPoint).FirstOrDefault(l => l.Any(w => !w.IsNotEnter));
                }

                Way lastWay = default;
                Way candidatWay = default;

                do
                {
                    candidatWay = candidatList.FirstOrDefault(w => w.OrderStatus != OrderStatus.Reserve && !w.IsNotEnter && w.AllowReverse);
                    if (candidatWay == null)
                    {
                        candidatWay = candidatList.FirstOrDefault(w => w.OrderStatus != OrderStatus.Reserve && !w.IsNotEnter);
                    }
                    
                    if (lastWay == null && candidatWay != null)
                    {
                        if (candidatList.Key != candidatWay.FirstNode)
                        {
                            if (candidatWay.AllowReverse)
                                candidatWay.ReverseDirection();
                            else
                                candidatWay = null;
                        }
                    }
                    if (candidatWay != null)
                    {
                        if (lastWay != null && lastWay.LastNode != candidatWay.FirstNode)
                        {
                            if (candidatWay.AllowReverse)
                                candidatWay.ReverseDirection();
                            else
                            {
                                candidatWay = null;
                                ways.ForEach(w => w.OrderStatus = OrderStatus.None);
                                skipEntryPoint++;
                                continue;
                            }
                        }

                        chainForward.Add(candidatWay);
                        candidatWay.OrderStatus = OrderStatus.Reserve;

                        lastWay = candidatWay;
                        var last = candidatWay.LastNode;
                        var exists = mapEdge[last];
                        if (exists.Any())
                            candidatList = (IGrouping<long, Way>)exists;
                        else
                        {
                            candidatList = null;
                            candidatWay = null;
                        }
                    }
                } while (candidatWay != null);

                (code, length) = CalculationStatisticsAndMatchMilestones(osmMilestones);
                if (code == MatchMilestonesErrorCode.NextEntryPoint)
                {
                    skipEntryPoint++;
                    continue;
                }
                else if (code == MatchMilestonesErrorCode.OK)
                {
                    break;
                }
                else if (code == MatchMilestonesErrorCode.NoMilestones)
                {
                    hasMilestone = false;
                    break;
                }
                else if (code == MatchMilestonesErrorCode.DublicateMilestones)
                {
                    length = 0;
                    break;
                }
            }

            var nouse = ways.Count(w => w.OrderStatus != OrderStatus.Reserve);
            return (chainForward.Count, nouse, length, hasMilestone);
        }

        private (MatchMilestonesErrorCode code, float length) CalculationStatisticsAndMatchMilestones(Dictionary<long, float> osmMilestones)
        {
            float length = 0f;
            errorsMilestonesDistance = new Dictionary<long, MilestoneMatch>();
            float lastMile = 0;
            float lastLength = 0;

            int nextLess = 0;
            int nextMore = 0;

            try
            {
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

                            errorsMilestonesDistance.Add(lastNode, new MilestoneMatch(mile, length));
                            lastMile = mile;
                        }
                        lastLength = length;
                    }
                }
            }
            catch (ArgumentException)
            {
                return (MatchMilestonesErrorCode.DublicateMilestones, 0);
            }

            if (nextLess > nextMore)
                return (MatchMilestonesErrorCode.NextEntryPoint, 0);

            if (errorsMilestonesDistance.Count > 0)
            {
                statStartMilestone = errorsMilestonesDistance.Values.Average(e => e.Error);

                if (errorsMilestonesDistance.Count > 4)
                {
                    (float outlierMin, float outlierMax) = Outlier.GetOutlierBoundary(errorsMilestonesDistance.Values.Select(e => e.Error), true);
                    foreach (var badMilestone in errorsMilestonesDistance.Values.Where(e => e.Error <= outlierMin || e.Error >= outlierMax))
                        badMilestone.IsBad = true;

                    statStartMilestone = errorsMilestonesDistance.Values.Where(e => !e.IsBad).Average(e => e.Error);
                }
                return (MatchMilestonesErrorCode.OK, length);
            }

            return (MatchMilestonesErrorCode.NoMilestones, length);
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
                        var milestoneMath = errorsMilestonesDistance[lastNode];
                        if (!milestoneMath.IsBad)
                        {
                            milestonePoints.Add(new MilestonePointWithError(mile, way.IsReverse ? edge.Start : edge.End, true)
                            {
                                Error = lengthTotal - mile
                            });

                            lengthTotal = mile;
                            nextMilestone = lengthTotal + MILESTONE_STEP_KM;
                        }
                    }
                }
            }

            return milestonePoints;
        }
    }
}
