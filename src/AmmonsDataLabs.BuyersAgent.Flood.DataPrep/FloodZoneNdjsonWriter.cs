using System.Text.Json;
using JetBrains.Annotations;
using NetTopologySuite.IO;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep;

public static class FloodZoneNdjsonWriter
{
    private static readonly WKBWriter WkbWriter = new() { Strict = false, HandleSRID = true };
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void Write(IEnumerable<FloodZone> zones, TextWriter writer)
    {
        foreach (var zone in zones)
        {
            var wkb = WkbWriter.Write(zone.Geometry);
            var base64 = Convert.ToBase64String(wkb);

            var obj = new FloodZoneRecord
            {
                Id = zone.Id,
                Risk = zone.Risk.ToString(),
                PolygonWkbBase64 = base64
            };

            var json = JsonSerializer.Serialize(obj, JsonOptions);
            writer.WriteLine(json);
        }
    }

    private sealed record FloodZoneRecord
    {
        public required string Id { [UsedImplicitly] get; init; }
        public required string Risk { [UsedImplicitly] get; init; }
        public required string PolygonWkbBase64 { [UsedImplicitly] get; init; }
    }
}
