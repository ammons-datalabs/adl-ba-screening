using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests.Helpers;

public static class SyntheticGeoJsonGenerator
{
    public static void GenerateBccFloodRiskGeoJson(string outputPath)
    {
        var factory = new GeometryFactory();

        var features = new List<Feature>
        {
            CreateFeature(factory, "1", "High",
                new Coordinate(153.00, -27.48),
                new Coordinate(153.05, -27.48),
                new Coordinate(153.05, -27.45),
                new Coordinate(153.00, -27.45)),

            CreateFeature(factory, "2", "Medium",
                new Coordinate(153.10, -27.50),
                new Coordinate(153.15, -27.50),
                new Coordinate(153.15, -27.47),
                new Coordinate(153.10, -27.47)),

            CreateFeature(factory, "3", "Low",
                new Coordinate(153.20, -27.52),
                new Coordinate(153.25, -27.52),
                new Coordinate(153.25, -27.49),
                new Coordinate(153.20, -27.49)),

            CreateFeature(factory, "4", "Very Low",
                new Coordinate(153.30, -27.54),
                new Coordinate(153.35, -27.54),
                new Coordinate(153.35, -27.51),
                new Coordinate(153.30, -27.51)),

            CreateFeature(factory, "5", "Extreme",
                new Coordinate(152.95, -27.46),
                new Coordinate(153.00, -27.46),
                new Coordinate(153.00, -27.43),
                new Coordinate(152.95, -27.43))
        };

        var featureCollection = new FeatureCollection();
        foreach (var feature in features)
        {
            featureCollection.Add(feature);
        }

        var serializer = GeoJsonSerializer.Create();
        using var writer = File.CreateText(outputPath);
        serializer.Serialize(writer, featureCollection);
    }

    public static void GenerateBccExtentsGeoJson(string outputPath)
    {
        var factory = new GeometryFactory();

        var features = new List<Feature>
        {
            CreateExtentFeature(factory, "101",
                new Coordinate(153.00, -27.48),
                new Coordinate(153.10, -27.48),
                new Coordinate(153.10, -27.40),
                new Coordinate(153.00, -27.40)),

            CreateExtentFeature(factory, "102",
                new Coordinate(153.10, -27.50),
                new Coordinate(153.20, -27.50),
                new Coordinate(153.20, -27.42),
                new Coordinate(153.10, -27.42))
        };

        var featureCollection = new FeatureCollection();
        foreach (var feature in features)
        {
            featureCollection.Add(feature);
        }

        var serializer = GeoJsonSerializer.Create();
        using var writer = File.CreateText(outputPath);
        serializer.Serialize(writer, featureCollection);
    }

    private static Feature CreateFeature(GeometryFactory factory, string objectId, string floodRisk, params Coordinate[] coords)
    {
        var ring = coords.ToList();
        ring.Add(ring[0]); // Close the ring

        var polygon = factory.CreatePolygon(ring.ToArray());
        polygon.SRID = 4326;

        var attributes = new AttributesTable
        {
            { "objectid", objectId },
            { "flood_risk", floodRisk },
            { "flood_type", "Creek River Storm Tide" }
        };

        return new Feature(polygon, attributes);
    }

    private static Feature CreateExtentFeature(GeometryFactory factory, string objectId, params Coordinate[] coords)
    {
        var ring = coords.ToList();
        ring.Add(ring[0]); // Close the ring

        var polygon = factory.CreatePolygon(ring.ToArray());
        polygon.SRID = 4326;

        var attributes = new AttributesTable
        {
            { "objectid", objectId }
        };

        return new Feature(polygon, attributes);
    }
}
