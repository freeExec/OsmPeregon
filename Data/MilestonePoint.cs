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
    }
}
