using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeExec.Geom
{
    public struct GeomPoint
    {
        public const double FACTOR = 1d / 10000000;

        public int LongitudeI;
        public int LatitudeI;

        public double Longitude => LongitudeI * FACTOR;
        public double Latitude => LatitudeI * FACTOR;

        public int this[int index]
        {
            get
            {
                return index switch
                {
                    0 => LongitudeI,
                    1 => LatitudeI,
                    _ => throw new IndexOutOfRangeException("Max 2")
                };
            }

            set
            {
                if (index == 0)
                    LongitudeI = value;
                else if (index == 1)
                    LatitudeI = value;
                else
                    throw new IndexOutOfRangeException("Max 2");
            }
        }

        public GeomPoint(int xLon, int yLat)
        {
            LongitudeI = xLon;
            LatitudeI = yLat;
        }

        public GeomPoint(int[] geom)
        {
            LongitudeI = geom[0];
            LatitudeI = geom[1];
        }

        public int[] GetArray() => new int[] { LongitudeI, LatitudeI };

        public static GeomPoint operator +(GeomPoint p1, GeomPoint p2)
        {
            return new GeomPoint(p1.LongitudeI + p2.LongitudeI, p1.LatitudeI + p2.LatitudeI);
        }

        public static GeomPoint operator -(GeomPoint p1, GeomPoint p2)
        {
            return new GeomPoint(p1.LongitudeI - p2.LongitudeI, p1.LatitudeI - p2.LatitudeI);
        }

        public static GeomPoint operator *(GeomPoint p1, float scale)
        {
            return new GeomPoint((int)(p1.LongitudeI * scale), (int)(p1.LatitudeI * scale));
        }
    }
}
