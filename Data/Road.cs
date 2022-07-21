using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsmPeregon.Data
{
    public class Road
    {
        private readonly List<long> wayIds;

        public readonly long Id;
        public readonly string Ref;
        public readonly string Name;

        public IReadOnlyList<long> WayIds => wayIds;

        private Way way;

        public Road(long id, string @ref, string name, IReadOnlyList<long> wayRefIds)
        {
            Id = id;
            Ref = @ref;
            Name = name;
            wayIds = new List<long>(wayRefIds);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Ref))
                return Name;
            else
                return $"{Name} ({Ref})";
        }

        public void AddWay(Way way)
        {
            this.way = way;
        }
    }
}
