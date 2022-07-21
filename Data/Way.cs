using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsmPeregon.Data
{
    public class Way
    {
        public readonly long Id;

        private readonly List<long> nodeIds;
        private readonly List<Edge> edges;

        public Way(long id, IReadOnlyCollection<long> nodeRefs)
        {
            Id = id;
            nodeIds = new List<long>(nodeRefs);
            edges = new List<Edge>(nodeIds.Count - 1);
        }
    }
}
