using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeExec.Tools
{
    public static class ExtendedTools
    {
        #region STD
        public static double Std(this IEnumerable<int> source)
        {
            var avg = source.Average();

            var std = 0.0;

            int count = 0;
            foreach (var val in source)
            {
                std += Math.Pow(val - avg, 2);
                count++;
            }

            std = std / count;

            return std;
        }

        public static float Std(this IEnumerable<float> source)
        {
            var avg = source.Average();

            var std = 0.0;

            int count = 0;
            foreach (var val in source)
            {
                std += Math.Pow(val - avg, 2);
                count++;
            }

            std = std / count;

            return (float)std;
        }

        public static double Std(this IEnumerable<double> source)
        {
            var avg = source.Average();

            var std = 0.0;

            int count = 0;
            foreach (var val in source)
            {
                std += Math.Pow(val - avg, 2);
                count++;
            }

            std = std / count;

            return std;
        }

        public static double Std<T>(this IEnumerable<T> source, Func<T, int> selector)
        {
            var avg = source.Average(selector);

            var std = 0.0;

            int count = 0;
            foreach (var val in source.Select(selector))
            {
                std += Math.Pow(val - avg, 2);
                count++;
            }

            std = std / count;

            return std;
        }

        public static float Std<T>(this IEnumerable<T> source, Func<T, float> selector)
        {
            var avg = source.Average(selector);

            var std = 0.0;

            int count = 0;
            foreach (var val in source.Select(selector))
            {
                std += Math.Pow(val - avg, 2);
                count++;
            }

            std = std / count;

            return (float)std;
        }

        public static double Std<T>(this IEnumerable<T> source, Func<T, double> selector)
        {
            var avg = source.Average(selector);

            var std = 0.0;

            int count = 0;
            foreach (var val in source.Select(selector))
            {
                std += Math.Pow(val - avg, 2);
                count++;
            }

            std = std / count;

            return std;
        }
        #endregion
    }
}
