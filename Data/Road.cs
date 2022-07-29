using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsmPeregon.Data
{
    public class Road
    {
        private readonly List<Way> ways;
        //private List<bool> reversed;
        private LinkedList<Way> chainForward;

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

            chainForward = new LinkedList<Way>();

            IEnumerable<Way> candidatList = mapEdge.Where(l => l.Count() == 1).First(l => l.Count(w => !w.IsBackward) > 0);
            Way candidatWay = default;

            do
            {

                candidatWay = candidatList.FirstOrDefault(w => w.OrderStatus != OrderStatus.Reserve && !w.IsBackward);
                if (candidatWay != null)
                {
                    chainForward.AddLast(candidatWay);
                    candidatWay.OrderStatus = OrderStatus.Reserve;
                    var last = candidatWay.LastNode;
                    candidatList = mapEdge[last];
                }

            } while (candidatWay != null);

            return chainForward.Count;
        }

        public int ReorderingWaysOld2()
        {
            //ways.ForEach(w => w.Reset());

            var t = ways
                .Select(w => Tuple.Create(w.Edges.First().NodeStart, w))
                .Concat(ways.Select(w => Tuple.Create(w.Edges.Last().NodeEnd, w)));
            var mapEdge = (Lookup<long, Way>)t.ToLookup(s => s.Item1, s => s.Item2);

            var chainForward = new LinkedList<Way>();
            var chainBackward = new List<Way>();

            foreach (var seg in ways)
            {
                bool allowForward = seg.DirectionRole == Direction.Both || seg.DirectionRole == Direction.Forward;
                bool allowReverse = seg.DirectionRole == Direction.Both;

                if (seg.OrderStatus == OrderStatus.Reserve)
                    continue;

                if (!allowForward)
                    continue;

                if (chainForward.Count == 0)
                {
                    seg.OrderStatus = OrderStatus.Reserve;
                    chainForward.AddLast(seg);
                    continue;
                }

                Way chainLastSegment;
                chainLastSegment = chainForward.Last();

                if (chainLastSegment.LastNode == seg.FirstNode)
                {
                    seg.OrderStatus = OrderStatus.Reserve;
                    chainForward.AddLast(seg);
                    continue;
                }
                else if (allowReverse && chainLastSegment.LastNode == seg.LastNode)
                {
                    seg.ReverseDirection();
                    seg.OrderStatus = OrderStatus.Reserve;
                    chainForward.AddLast(seg);
                    continue;
                }

                Way chainFirstSegment;
                chainFirstSegment = chainForward.First();

                if (chainFirstSegment.FirstNode == seg.LastNode)
                {
                    seg.OrderStatus = OrderStatus.Reserve;
                    chainForward.AddFirst(seg);
                    continue;
                }
                else if (allowReverse && chainFirstSegment.FirstNode == seg.FirstNode)
                {
                    seg.ReverseDirection();
                    seg.OrderStatus = OrderStatus.Reserve;
                    chainForward.AddFirst(seg);
                    continue;
                }

                // два направления поиска звеньев - влево(1) и вправо(2)
                int directionSearch = 1;
                do
                {
                    //WaySegment prevCondidatWay;
                    chainLastSegment = chainForward.Last();

                    var candidatList = mapEdge[chainLastSegment.LastNode];
                    Way candidatWay = candidatList.FirstOrDefault(w => w.OrderStatus != OrderStatus.Reserve);
                    int candidatListCount = candidatList.Count();

                    if (/*candidatListCount != 2 && !(chain.Count == 0 && candidatListCount > 2) ||*/ candidatWay == null)
                    {
                        //if (chain.Count == 0) break;

                        if (directionSearch == 1)
                        {
                            chainForward.Reverse();
//                            chainForward.ForEach(w => w.ReverseDirection());
                        }

                        directionSearch++;
                        continue;
                    }

                    candidatWay.OrderStatus = OrderStatus.Reserve;

                    if (chainLastSegment.LastNode == candidatWay.LastNode)
                    {
                        candidatWay.ReverseDirection();
                    }

                    chainForward.AddLast(candidatWay);

                } while (directionSearch <= 2);

                if (chainForward.Count > 0)
                {
                    //_globalSegments.Add(new WaySegment(-1, chain.First().First, chain.Last().Last));
                    int gg = 99;
                }

                chainForward.Clear();

                if (seg.OrderStatus != OrderStatus.Reserve)
                {
                    seg.OrderStatus = OrderStatus.Reserve;
                    chainForward.AddLast(seg);
                }
            }

            //if (_globalSegments.Count == 0 && chain.Count > 0)
            //{
            //    _globalSegments.Add(new WaySegment(-1, chain.First().First, chain.Last().Last));
            //}

            //return _globalSegments.Count;
            return 0;
        }

        public int ReorderingWaysBack()
        {
            //ways.ForEach(w => w.Reset());

            var t = ways
                .Select(w => Tuple.Create(w.Edges.First().NodeStart, w))
                .Concat(ways.Select(w => Tuple.Create(w.Edges.Last().NodeEnd, w)));
            var mapEdge = (Lookup<long, Way>)t.ToLookup(s => s.Item1, s => s.Item2);

            var chainForward = new List<Way>();
            var chainBackward = new List<Way>();

            foreach (var seg in ways)
            {
                bool allowForward = seg.DirectionRole == Direction.Both || seg.DirectionRole == Direction.Forward;

                if (seg.OrderStatus == OrderStatus.Reserve)
                    continue;

                if (!allowForward)
                    continue;

                if (chainForward.Count == 0)
                {
                    seg.OrderStatus = OrderStatus.Reserve;
                    chainForward.Add(seg);
                    continue;
                }

                Way prevCondidatWay;
                prevCondidatWay = chainForward.Last();

                if (prevCondidatWay.LastNode == seg.FirstNode)
                {
                    seg.OrderStatus = OrderStatus.Reserve;
                    chainForward.Add(seg);
                    continue;
                }
                else if (prevCondidatWay.LastNode == seg.LastNode)
                {
                    //seg.ReverseDirection();
                    seg.OrderStatus = OrderStatus.Reserve;
                    chainForward.Insert(0, seg);
                    continue;
                }

                // два направления поиска звеньев - влево(1) и вправо(2)
                int directionSearch = 1;
                do
                {
                    //WaySegment prevCondidatWay;
                    prevCondidatWay = chainForward.Last();

                    var candidatList = mapEdge[prevCondidatWay.LastNode];
                    Way candidatWay = candidatList.FirstOrDefault(w => w.OrderStatus != OrderStatus.Reserve);
                    int candidatListCount = candidatList.Count();

                    if (/*candidatListCount != 2 && !(chain.Count == 0 && candidatListCount > 2) ||*/ candidatWay == null)
                    {
                        //if (chain.Count == 0) break;

                        if (directionSearch == 1)
                        {
                            chainForward.Reverse();
                            chainForward.ForEach(w => w.ReverseDirection());
                        }

                        directionSearch++;
                        continue;
                    }

                    candidatWay.OrderStatus = OrderStatus.Reserve;

                    if (prevCondidatWay.LastNode == candidatWay.LastNode)
                    {
                        candidatWay.ReverseDirection();
                    }

                    chainForward.Add(candidatWay);

                } while (directionSearch <= 2);

                if (chainForward.Count > 0)
                {
                    //_globalSegments.Add(new WaySegment(-1, chain.First().First, chain.Last().Last));
                    int gg = 99;
                }

                chainForward.Clear();

                if (seg.OrderStatus != OrderStatus.Reserve)
                {
                    seg.OrderStatus = OrderStatus.Reserve;
                    chainForward.Add(seg);
                }
            }

            //if (_globalSegments.Count == 0 && chain.Count > 0)
            //{
            //    _globalSegments.Add(new WaySegment(-1, chain.First().First, chain.Last().Last));
            //}

            //return _globalSegments.Count;
            return 0;
        }
    }
}
