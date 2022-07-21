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

        private List<Edge> edges;

        public ICollection<Edge> Edges => edges;

        public Way(long id)
        {
            Id = id;
        }

        public void AddEdges(IEnumerable<Edge> edges)
        {
            this.edges = edges.ToList();
        }

        public override string ToString() => $"W{Id} - Edge: {edges?.Count ?? 0}";
    }
}
