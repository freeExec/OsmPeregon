using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeExec.Geom;
using OsmPeregon.Data;

namespace OsmPeregon
{
    public static class GeojsonGenerator
    {
        private static StringBuilder sbGeojson = new StringBuilder(4096 * 16);

        public static string FromMilestones(IEnumerable<MilestonePoint> milestonePoints)
        {
            InternalGeojsonPreambule();
            var props = new Dictionary<string, object>();

            MilestonePoint prevMilestone = milestonePoints.First();

            foreach (var milestoneCollect in milestonePoints.GroupBy(m => m.Milestone).OrderBy(m => m.Key))
            {
                var milestone = milestoneCollect.FirstOrDefault(m => m.IsOriginal);
                if (milestone == default)
                    milestone = milestoneCollect.First();

                float distanceFromPrevMilestone = 1f - (float)H3.GeoTools.GeoDistKm(prevMilestone.GeomPosition, milestone.GeomPosition);

                props["label"] = milestone.Milestone.ToString();
                props["delta"] = distanceFromPrevMilestone.ToString("F3");
                AddPoint(milestone.GeomPosition, props);

                prevMilestone = milestone;
            }

            InternalGeojsonConclusion();
            return sbGeojson.ToString();
        }

        public static string FromNodeIds(IEnumerable<(long id, GeomPoint geom, string value)> badMilestones)
        {
            InternalGeojsonPreambule();
            var props = new Dictionary<string, object>();
            foreach (var node in badMilestones)
            {
                props["name"] = node.value;
                props["osmid"] = node.id;
                AddPoint(node.geom, props);
            }
            InternalGeojsonConclusion();
            return sbGeojson.ToString();
        }

        private static void AddPoint(GeomPoint geom, IEnumerable<KeyValuePair<string, object>> props)
        {
            sbGeojson.Append("{\"type\":\"Feature\",\"properties\":{");
            foreach (var pr in props)
            {
                if (pr.Value is string)
                    sbGeojson.Append($"\"{pr.Key}\":\"{pr.Value}\",");
                else if (pr.Value is long || pr.Value is int || pr.Value is float)
                    sbGeojson.Append($"\"{pr.Key}\":{pr.Value},");
            }
            sbGeojson.Length--;
            sbGeojson.Append("},");
            sbGeojson.Append($"\"geometry\":{{\"type\":\"Point\",\"coordinates\":[{geom.Longitude:F6},{geom.Latitude:F6}]}}}}");
            sbGeojson.AppendLine(",");
        }

        private static void InternalGeojsonPreambule()
        {
            sbGeojson.Length = 0;
            sbGeojson.AppendLine("{\"type\": \"FeatureCollection\",\"features\": [");
        }

        private static void InternalGeojsonConclusion()
        {
            sbGeojson.Length -= Environment.NewLine.Length + 1;
            sbGeojson.AppendLine("]}");

        }
    }
}
