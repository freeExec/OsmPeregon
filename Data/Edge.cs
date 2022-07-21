using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using H3;

namespace OsmPeregon.Data
{
    public record Edge
    {
        public int[] Start;
        public int[] End;

        public readonly long NodeStart;
        public readonly long NodeEnd;

        private float? length;
        public float Length
        {
            get 
            {
                if (!length.HasValue)
                    length = (float)GeoTools.GeoDistKm(Start, End);
                return length.Value;
            }
        }

        public Edge(long start, long end)
        {
            NodeStart = start;
            NodeEnd = end;
        }
    }
}
