using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep;

public static class ShapefileFloodZoneReader
{
    public delegate (string id, FloodRisk risk) AttributeMapper(IDictionary<string, object?> attributes);

    public static IEnumerable<FloodZone> Read(string shapefilePath, AttributeMapper mapper)
    {
        using var reader = new ShapefileDataReader(shapefilePath, new GeometryFactory());

        var header = reader.DbaseHeader;
        var fieldNames = Enumerable.Range(0, header.NumFields)
            .Select(i => header.Fields[i].Name)
            .ToArray();

        while (reader.Read())
        {
            var geom = reader.Geometry;
            if (geom is null)
                continue;

            var attributes = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < fieldNames.Length; i++)
            {
                attributes[fieldNames[i]] = reader.GetValue(i + 1); // index 0 is geometry
            }

            var (id, risk) = mapper(attributes);

            yield return new FloodZone
            {
                Id = id,
                Risk = risk,
                Geometry = geom
            };
        }
    }
}
