using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests.Helpers;

public static class SyntheticShapefileGenerator
{
    public static void GenerateBccFloodShapefile(string outputPath)
    {
        var factory = new GeometryFactory();

        var features = new List<Feature>
        {
            CreateFeature(factory, 1, "High",
                new Coordinate(153.00, -27.48),
                new Coordinate(153.05, -27.48),
                new Coordinate(153.05, -27.45),
                new Coordinate(153.00, -27.45)),

            CreateFeature(factory, 2, "Medium",
                new Coordinate(153.10, -27.50),
                new Coordinate(153.15, -27.50),
                new Coordinate(153.15, -27.47),
                new Coordinate(153.10, -27.47)),

            CreateFeature(factory, 3, "Low",
                new Coordinate(153.20, -27.52),
                new Coordinate(153.25, -27.52),
                new Coordinate(153.25, -27.49),
                new Coordinate(153.20, -27.49))
        };

        var shapefileWriter = new ShapefileDataWriter(outputPath, factory)
        {
            Header = CreateHeader()
        };

        shapefileWriter.Write(features);
    }

    private static Feature CreateFeature(GeometryFactory factory, int objectId, string likelihood, params Coordinate[] coords)
    {
        var ring = coords.ToList();
        ring.Add(ring[0]); // Close the ring

        var polygon = factory.CreatePolygon(ring.ToArray());
        polygon.SRID = 4326;

        var attributes = new AttributesTable
        {
            { "OBJECTID", objectId },
            { "LIKELIHOOD", likelihood }
        };

        return new Feature(polygon, attributes);
    }

    private static DbaseFileHeader CreateHeader()
    {
        var header = new DbaseFileHeader();
        header.AddColumn("OBJECTID", 'N', 10, 0);
        header.AddColumn("LIKELIHOOD", 'C', 20, 0);
        return header;
    }
}
