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

    public class MilestonePointToInsertOsm : MilestonePoint
    {
        public Way Way;
        public Edge Edge;
        public long OsmId;

        public MilestonePointToInsertOsm(float mile, GeomPoint geom, bool isOriginal)
            : base(mile, geom, isOriginal)
        {

        }
    }
}
