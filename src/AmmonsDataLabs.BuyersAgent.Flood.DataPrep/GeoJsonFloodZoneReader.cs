using NetTopologySuite.Features;
using NetTopologySuite.IO;
using Newtonsoft.Json;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep;

public static class GeoJsonFloodZoneReader
{
    public delegate (string id, FloodRisk risk) AttributeMapper(IAttributesTable attributes);

    public static IEnumerable<FloodZone> Read(string geoJsonPath, AttributeMapper mapper)
    {
        using var stream = File.OpenRead(geoJsonPath);
        return ReadCore(stream, mapper);
    }

    public static IEnumerable<FloodZone> Read(Stream stream, AttributeMapper mapper)
    {
        return ReadCore(stream, mapper);
    }

    private static List<FloodZone> ReadCore(Stream stream, AttributeMapper mapper)
    {
        var serializer = GeoJsonSerializer.Create();
        using var streamReader = new StreamReader(stream);
        using var jsonReader = new JsonTextReader(streamReader);

        var featureCollection = serializer.Deserialize<FeatureCollection>(jsonReader);
        if (featureCollection is null)
            return [];

        var zones = new List<FloodZone>();
        foreach (var feature in featureCollection)
        {
            var geom = feature.Geometry;
            if (geom is null)
                continue;

            var (id, risk) = mapper(feature.Attributes);

            zones.Add(new FloodZone
            {
                Id = id,
                Risk = risk,
                Geometry = geom
            });
        }

        return zones;
    }
}