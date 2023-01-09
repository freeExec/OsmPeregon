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
        public OrderStatus OrderStatus;
        public bool IsReverse { get; private set; }

        public bool AllowReverse => DirectionRole == Direction.Both;
        public bool IsNotEnter => !AllowReverse && IsReverse;

        public ICollection<Edge> EdgesRaw => edges;

        public IEnumerable<Edge> Edges => IsReverse ? EdgesRaw.Reverse() : EdgesRaw;

        public long FirstNodeRaw => edges.First().NodeStart;
        public long LastNodeRaw => edges.Last().NodeEnd;

        public long FirstNode => IsReverse ? LastNodeRaw : FirstNodeRaw;
        public long LastNode => IsReverse ? FirstNodeRaw : LastNodeRaw;

        public bool IsCorrect => EdgesRaw != null && EdgesRaw.All(e => e.IsCorrect);

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
            if (DirectionRole == Direction.Backward)
                IsReverse = true;
        }

        public void AddEdges(IEnumerable<Edge> edges)
        {
            this.edges = edges.ToList();
        }

        public void ReverseDirection()
        {
            if (!AllowReverse)
                throw new NotSupportedException("Not allow reversing");
            IsReverse = !IsReverse;
        }

        public void SetOneway(string onewayTag)
        {
            if (string.IsNullOrEmpty(onewayTag) || DirectionRole != Direction.Both)
                return;
            
            if (onewayTag == "yes" || onewayTag == "1")
            {
                DirectionRole = Direction.Forward;
            }
            else if (onewayTag == "-1")
            {
                IsReverse = true;
                DirectionRole = Direction.Backward;
            }            
        }

        public override string ToString()
        {
            if (IsCorrect)
                return $"W{Id} - Edge: {edges?.Count ?? 0} {(!IsCorrect ? '-' : ' ')} - {DirectionRoleChar} [{FirstNode}-{LastNode}] {(OrderStatus == OrderStatus.Reserve ? 'v' : ' ')}";
            else
                return $"W{Id} - Edge: {edges?.Count ?? 0} {(!IsCorrect ? '-' : ' ')} - {DirectionRoleChar}";
        }

        private char DirectionRoleChar => DirectionRole switch { Direction.Forward => '↑', Direction.Backward => '↓', Direction.Both => '⇅', _ => 'X' };
    }
}
