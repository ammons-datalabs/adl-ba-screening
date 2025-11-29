using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace AmmonsDataLabs.BuyersAgent.Geo;

public static class GeoFactory
{
    private static readonly GeometryFactory Factory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    public static Point CreatePoint(GeoPoint point) =>
        Factory.CreatePoint(new Coordinate(point.Longitude, point.Latitude));

    public static Polygon CreatePolygon(params GeoPoint[] vertices)
    {
        if (vertices.Length < 3)
            throw new ArgumentException("At least three vertices are required to create a polygon.", nameof(vertices));

        var coords = vertices
            .Select(v => new Coordinate(v.Longitude, v.Latitude))
            .ToList();

        if (!coords.First().Equals2D(coords.Last()))
            coords.Add(coords.First());

        var shell = Factory.CreateLinearRing([.. coords]);
        return Factory.CreatePolygon(shell);
    }
}
