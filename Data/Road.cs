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
    }
}
