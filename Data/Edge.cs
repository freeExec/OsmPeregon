using FreeExec.Geom;
using H3;

namespace OsmPeregon.Data
{
    public class Edge
    {
        public GeomPoint Start;
        public GeomPoint End;

        public readonly long NodeStart;
        public readonly long NodeEnd;

        private float? length;
        public float Length
        {
            get
            {
                if (!length.HasValue)
                    length = (float)GeoTools.GeoDistKm(Start, End);
                return length.Value;
            }
        }

        public bool IsCorrect => !Start.IsEmpty || !End.IsEmpty;

        public Edge(long start, long end)
        {
            NodeStart = start;
            NodeEnd = end;
            //Start = GeomPoint.Empty;
            //End = GeomPoint.Empty;
            //length = null;
        }

        public GeomPoint InterpolatePosition(float lineFactor) => Start + (End - Start) * lineFactor;

        public override string ToString()
        {
            return $"{NodeStart}{(Start.IsEmpty ? "(-)" : "")}-{NodeEnd}{(End.IsEmpty ? "(-)" : "")}";
        }
    }
}
