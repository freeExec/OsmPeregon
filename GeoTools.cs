/*
 * Copyright 2022 freeExec
 * Copyright 2016-2017 Uber Technologies, Inc.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *         http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using FreeExec.Geom;

namespace H3
{
    public enum GeographicIndex
    {
        Longitude = 0,
        Latitude = 1,
    }

    public static class GeoTools
    {
        public const double M_PI_180 = Math.PI / 180d;
        const double M_180_PI = Math.PI * 180d;
        const double M_PI = Math.PI;
        const double M_PI_2 = Math.PI / 2d;

        /// <summary>
        /// earth radius in kilometers using WGS84 authalic radius
        /// </summary>
        const double EARTH_RADIUS_KM = 6371.007180918475d;

        const double EPSILON_DEG = 0.000000001;
        const double EPSILON_RAD = (EPSILON_DEG * M_PI_180);

        public const double FACTOR = 1d / 10000000;

        /// <summary>
        /// Convert from decimal degrees to radians.
        /// </summary>
        /// <param name="degrees">The decimal degrees.</param>
        /// <returns>The corresponding radians.</returns>
        public static double DegsToRads(double degrees)
        {
            return degrees * M_PI_180;
        }

        /// <summary>
        /// Convert from radians to decimal degrees.
        /// </summary>
        /// <param name="radians">The radians.</param>
        /// <returns>The corresponding decimal degrees.</returns>
        public static double RadsToDegs(double radians)
        {
            return radians * M_180_PI;
        }

        /// Find the great circle distance in radians between two spherical coordinates.
        /// </summary>
        /// <param name="p2">The second spherical coordinates.</param>
        /// <returns>The great circle distance in radians between this and p2.</returns>
        public static double GeoDistRads(int[] p1, int[] p2)
        {
            double lon = DegsToRads(p1[(int)GeographicIndex.Longitude] * FACTOR);
            double lat = DegsToRads(p1[(int)GeographicIndex.Latitude] * FACTOR);
            double p2lon = DegsToRads(p2[(int)GeographicIndex.Longitude] * FACTOR);
            double p2lat = DegsToRads(p2[(int)GeographicIndex.Latitude] * FACTOR);

            return GeoDistRads(lon, lat, p2lon, p2lat);
        }
        private static double GeoDistRads(double lon, double lat, double p2lon, double p2lat)
        {
            // use spherical triangle with p1 at A, p2 at B, and north pole at C
            double bigC = Math.Abs(p2lon - lon);
            if (bigC > Math.PI)  // assume we want the complement
            {
                // note that in this case they can't both be negative
                double lon1 = lon;
                if (lon1 < 0.0d) lon1 += 2.0d * M_PI;
                double lon2 = p2lon;
                if (lon2 < 0.0d) lon2 += 2.0d * M_PI;

                bigC = Math.Abs(lon2 - lon1);
            }

            double b = M_PI_2 - lat;
            double a = M_PI_2 - p2lat;

            // use law of cosines to find c
            double cosc = Math.Cos(a) * Math.Cos(b) + Math.Sin(a) * Math.Sin(b) * Math.Cos(bigC);
            if (cosc > 1.0d) cosc = 1.0d;
            if (cosc < -1.0d) cosc = -1.0d;

            return Math.Acos(cosc);
        }

        /// <summary>
        /// Find the great circle distance in kilometers between two spherical
        /// coordinates.
        /// </summary>
        /// <param name="p2">The second spherical coordinates.</param>
        /// <returns>The distance in kilometers between p1 and p2.</returns>
        public static double GeoDistKm(int[] p1, int[] p2)
        {
            return EARTH_RADIUS_KM * GeoDistRads(p1, p2);
        }

        /// <summary>
        /// Find the great circle distance in kilometers between two spherical
        /// coordinates.
        /// </summary>
        /// <param name="p2">The second spherical coordinates.</param>
        /// <returns>The distance in kilometers between p1 and p2.</returns>
        public static double GeoDistKm(GeomPoint p1, GeomPoint p2)
        {
            return EARTH_RADIUS_KM * GeoDistRads(
                DegsToRads(p1.Longitude), DegsToRads(p1.Latitude),
                DegsToRads(p2.Longitude), DegsToRads(p2.Latitude)
            );
        }
    }
}