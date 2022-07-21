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
        public Direction DirectionRole;

        public ICollection<Edge> Edges => edges;

        public Way(long id, string role)
        {
            Id = id;
            DirectionRole = role switch
            {
                "forward" => Direction.Forward,
                "backward" => Direction.Backward,
                "" => Direction.Both,
                _ => Direction.NotSet
            };
        }

        public void AddEdges(IEnumerable<Edge> edges)
        {
            this.edges = edges.ToList();
        }

        public override string ToString() => $"W{Id} - Edge: {edges?.Count ?? 0} - {DirectionRoleChar}";

        private char DirectionRoleChar => DirectionRole switch { Direction.Forward => '↑', Direction.Backward => '↓', Direction.Both => '⇅', _ => 'X' };
    }
}
