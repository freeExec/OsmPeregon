using System;
using System.Linq;
using System.Collections.Generic;
using FormatsOsm;
using OsmPeregon.Data;

namespace OsmPeregon
{
    class Program
    {

        static void Main(string[] args)
        {
            //var o5mSource = @"d:\frex\Test\OSM\RU_local\highway_road.o5m";
            var o5mSource = "relation-ural-ulyanovsk.o5m";
            var o5mReader = new O5mStreamReader(o5mSource);

            var roadDictionary = new Dictionary<long, Road>(27000);

            foreach (var reader in o5mReader.SectionRelation)
            {
                if (reader.Contains(OsmConstants.KEY_TYPE, OsmConstants.KEY_ROUTE)
                    && reader.Contains(OsmConstants.KEY_ROUTE, OsmConstants.TAG_ROAD))
                {
                    var road = new Road(
                        reader.Id,
                        reader.GetTagValue(OsmConstants.KEY_REF),
                        reader.GetTagValue(OsmConstants.KEY_NAME),
                        reader.Refs
                    );

                    roadDictionary.Add(road.Id, road);
                }
            }



            //var way2roadLink = roadDictionary.Values.ToLookup(r => r.WayIds, r => r.Id);
            var way2roadLink = new Dictionary<long, List<Road>>();
            foreach (var road in roadDictionary.Values)
            {
                foreach (var wId in road.WayIds)
                {
                    List<Road> roadsWayContains;
                    if (!way2roadLink.TryGetValue(wId, out roadsWayContains))
                    {
                        roadsWayContains = new List<Road>();
                        way2roadLink.Add(wId, roadsWayContains);
                    }
                    roadsWayContains.Add(road);
                }
            }



            foreach (var reader in o5mReader.SectionWay)
            {
                if (way2roadLink.TryGetValue(reader.Id, out List<Road> roadsWayContains))
                {
                    var way = new Way(reader.Id, reader.Refs);
                    foreach (var road in roadsWayContains)
                    {
                        road.AddWay(way);
                    }
                }
            }
        }
    }
}
