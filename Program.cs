﻿#define LOCAL

using System;
using System.Linq;
using System.Collections.Generic;
using FormatsOsm;
using OsmPeregon.Data;

namespace OsmPeregon
{
    class Program
    {
#if LOCAL
        private const int OSM_ROAD_COUNT = 10;
        private const int OSM_WAY_COUNT = 200;
        private const int OSM_EDGE_COUNT = 1000;
#else
        private const int OSM_ROAD_COUNT = 27000;
        private const int OSM_WAY_COUNT = 100000;
        private const int OSM_EDGE_COUNT = 100000;
#endif

        static void Main(string[] args)
        {
            //var o5mSource = @"d:\frex\Test\OSM\RU_local\highway_road.o5m";
            //var o5mSource = "relation-ural-ulyanovsk.o5m";
            var o5mSource = "test-road.o5m";
            var o5mReader = new O5mStreamReader(o5mSource);

            var roadDictionary = new Dictionary<long, Road>(OSM_ROAD_COUNT);
            var wayDictionary = new Dictionary<long, Way>(OSM_WAY_COUNT);

            foreach (var reader in o5mReader.SectionRelation)
            {
                if (reader.Contains(OsmConstants.KEY_TYPE, OsmConstants.KEY_ROUTE)
                    && reader.Contains(OsmConstants.KEY_ROUTE, OsmConstants.TAG_ROAD))
                {
                    var road = new Road(
                        reader.Id,
                        reader.GetTagValue(OsmConstants.KEY_REF),
                        reader.GetTagValue(OsmConstants.KEY_NAME),
                        reader.Members.Select(member => new Way(member.Id, member.Role))
                    );

                    foreach (var way in road.Ways)
                        wayDictionary.Add(way.Id, way);

                    roadDictionary.Add(road.Id, road);
                }
            }

            var edgeDictionary = new Dictionary<long, List<Edge>>(OSM_EDGE_COUNT);

            foreach (var reader in o5mReader.SectionWay)
            {
                if (wayDictionary.TryGetValue(reader.Id, out Way way))
                {
                    way.AddEdges(reader.Refs.Pairwise().Select(pair => new Edge(pair.Previous, pair.Current)));
                    foreach (var edge in way.Edges)
                    {
                        Action<long, Edge> addNodeForEdge = (long nodeId, Edge edg) =>
                        {
                            List<Edge> nodeForEdges;
                            if (!edgeDictionary.TryGetValue(nodeId, out nodeForEdges))
                            {
                                nodeForEdges = new List<Edge>();
                                edgeDictionary.Add(nodeId, nodeForEdges);
                            }
                            nodeForEdges.Add(edg);
                        };

                        addNodeForEdge(edge.NodeStart, edge);
                        addNodeForEdge(edge.NodeEnd, edge);
                    }
                }
            }

            int gg = roadDictionary.Values.First().ReorderingWays();

            foreach (var record in o5mReader.SectionNode)
            {
                if (edgeDictionary.TryGetValue(record.Id, out List<Edge> edges))
                {
                    var coord = new int[] { record.LonI, record.LatI };
                    foreach (var edge in edges)
                    {
                        if (record.Id == edge.NodeStart)
                            edge.Start = coord;
                        if (record.Id == edge.NodeEnd)
                            edge.End = coord;
                    }
                }
            }
        }
    }
}