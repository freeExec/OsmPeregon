using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmPeregon.Data;

namespace OsmPeregon
{
    public static class GeojsonGenerator
    {
        private static StringBuilder sbGeojson = new StringBuilder(4096 * 16);

        public static string FromMilestones(IEnumerable<MilestonePoint> milestonePoints)
        {
            sbGeojson.Length = 0;
            sbGeojson.AppendLine("{\"type\": \"FeatureCollection\",\"features\": [");

            MilestonePoint prevMilestone = milestonePoints.First();

            foreach (var milestoneCollect in milestonePoints.GroupBy(m => m.Milestone).OrderBy(m => m.Key))
            {
                var milestone = milestoneCollect.FirstOrDefault(m => m.IsOriginal);
                if (milestone == default)
                    milestone = milestoneCollect.First();

                float distanceFromPrevMilestone = 1f - (float)H3.GeoTools.GeoDistKm(prevMilestone.GeomPosition, milestone.GeomPosition);

                sbGeojson.Append($"{{\"type\":\"Feature\",\"properties\":{{ \"label\":\"{milestone.Milestone}\", \"delta\":{distanceFromPrevMilestone:F3}}},");
                sbGeojson.Append($"\"geometry\":{{\"type\":\"Point\",\"coordinates\":[{milestone.GeomPosition.Longitude:F6},{milestone.GeomPosition.Latitude:F6}]}}}}");
                sbGeojson.AppendLine(",");

                prevMilestone = milestone;
            }

            sbGeojson.Length -= Environment.NewLine.Length + 1;
            sbGeojson.AppendLine("]}");
            return sbGeojson.ToString();
        }
    }
}
