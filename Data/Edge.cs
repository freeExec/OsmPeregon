using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsmPeregon.Data
{
    public record Edge
    {
        public int[] Start;
        public int[] End;

        public readonly long NodeStart;
        public readonly long NodeEnd;

        public Edge(long start, long end)
        {
            NodeStart = start;
            NodeEnd = end;
        }
    }
}
