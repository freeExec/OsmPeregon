using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsmPeregon
{
    public static class Outlier
    {
        public static (float min, float max) GetOutlierBoundary(IEnumerable<float> source, bool otlierMajor = true)
        {
            var sort = source.OrderBy(x => x).ToList();

            (int q2I1, int q2I2) = GetIndexForQ(sort.Count);
            float q2 = (sort[q2I1] + sort[q2I2]) / 2f;

            (int q1I1, int q1I2) = GetIndexForQ(q2I1);
            float q1 = (sort[q1I1] + sort[q1I2]) / 2f;

            float q3 = (sort[q2I1 + q1I1] + sort[q2I1 + q1I2]) / 2f;

            float iqr = q3 - q1;


            float otlierMin = q1 - iqr * (otlierMajor ? 3f : 1.5f);
            float otlierMax = q3 + iqr * (otlierMajor ? 3f : 1.5f);

            return (otlierMin, otlierMax);
        }

        private static (int, int) GetIndexForQ(int count)
        {
            if (count % 2 == 0)
            {
                int qI1 = (count + 1) / 2;
                int qI2 = qI1 - 1;

                return (qI1, qI2);
            }
            else
            {
                int qI = (count + 1) / 2 - 1;
                return (qI, qI);
            }
        }
    }
}
