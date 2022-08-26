using FreeExec.Geom;

namespace OsmPeregon.Data
{
    public class MilestonePoint
    {
        public float Milestone;
        public GeomPoint GeomPosition;
        public bool IsOriginal;

        public MilestonePoint(float mile, GeomPoint geom, bool isOriginal)
        {
            Milestone = mile;
            GeomPosition = geom;
            IsOriginal = isOriginal;
        }

        public override string ToString()
        {
            return $"{Milestone} {(IsOriginal ? "o" : "")}";
        }
    }

    public class MilestonePointWithError : MilestonePoint
    {
        public float Error;

        public MilestonePointWithError(float mile, GeomPoint geom, bool isOriginal)
            : base(mile, geom, isOriginal)
        {
        }
    }

    public class MilestoneMatch
    {
        public float OriginalDistance;
        public float RealDistance;
        public bool IsBad;

        public float Error => OriginalDistance - RealDistance;

        public MilestoneMatch(float orig, float real)
        {
            OriginalDistance = orig;
            RealDistance = real;
        }

        public override string ToString()
        {
            return $"{OriginalDistance} ({Error}) {(IsBad ? "X" : "")}";
        }
    }
}
