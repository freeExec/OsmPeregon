using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsmPeregon.Data
{
    public class Road
    {
        public enum OrderStatus
        {
            None,
            Reserve,
        }

        private readonly List<Way> ways;
        private List<bool> reversed;

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
            this.reversed = new List<bool>(this.ways.Count);
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
            //ways.ForEach(w => w.Reset());

            var t = ways
                .Select(w => Tuple.Create(w.Edges.First().NodeStart, w))
                .Concat(ways.Select(w => Tuple.Create(w.Edges.Last().NodeEnd, w)));
            var mapEdge = (Lookup<long, Way>)t.ToLookup(s => s.Item1, s => s.Item2);

            var chain = new List<Way>();

            foreach (var seg in ways)
            {
                if (seg.Status == WaySegment.OrderStatus.Reserve)
                    continue;

                if (chain.Count == 0)
                {
                    seg.Status = WaySegment.OrderStatus.Reserve;
                    chain.Add(seg);
                    continue;
                }

            //    WaySegment prevCondidatWay;
            //    prevCondidatWay = chain.Last();

            //    if (prevCondidatWay.Last == seg.First)
            //    {
            //        seg.Status = WaySegment.OrderStatus.Reserve;
            //        chain.Add(seg);
            //        continue;
            //    }
            //    else if (prevCondidatWay.Last == seg.Last)
            //    {
            //        seg.ReverseDirection();
            //        seg.Status = WaySegment.OrderStatus.Reserve;
            //        chain.Add(seg);
            //        continue;
            //    }

            //    // два направления поиска звеньев - влево(1) и вправо(2)
            //    int directionSearch = 1;
            //    do
            //    {
            //        //WaySegment prevCondidatWay;
            //        prevCondidatWay = chain.Last();

            //        var candidatList = mapEdge[prevCondidatWay.Last];
            //        WaySegment candidatWay = candidatList.FirstOrDefault(w => w.Status != WaySegment.OrderStatus.Reserve);
            //        int candidatListCount = candidatList.Count();

            //        if (/*candidatListCount != 2 && !(chain.Count == 0 && candidatListCount > 2) ||*/ candidatWay == null)
            //        {
            //            //if (chain.Count == 0) break;

            //            if (directionSearch == 1)
            //            {
            //                chain.Reverse();
            //                chain.ForEach(w => w.ReverseDirection());
            //            }

            //            directionSearch++;
            //            continue;
            //        }

            //        candidatWay.Status = WaySegment.OrderStatus.Reserve;

            //        if (prevCondidatWay.Last == candidatWay.Last)
            //        {
            //            candidatWay.ReverseDirection();
            //        }

            //        chain.Add(candidatWay);

            //    } while (directionSearch <= 2);

            //    if (chain.Count > 0)
            //    {
            //        _globalSegments.Add(new WaySegment(-1, chain.First().First, chain.Last().Last));
            //    }

            //    chain.Clear();

            //    if (seg.Status != WaySegment.OrderStatus.Reserve)
            //    {
            //        seg.Status = WaySegment.OrderStatus.Reserve;
            //        chain.Add(seg);
            //    }
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
