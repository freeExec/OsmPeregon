using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsmPeregon.Data
{
    public record Edge
    {
        public readonly int[] Start;
        public readonly int[] End;

        public readonly long NodeStart;
        public readonly long NodeEnd;
    }
}
