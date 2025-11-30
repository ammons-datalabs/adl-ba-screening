using System.Text.Json;
using AmmonsDataLabs.BuyersAgent.Flood.Configuration;
using NetTopologySuite.IO;

namespace AmmonsDataLabs.BuyersAgent.Flood;

public sealed class NdjsonFloodZoneDataLoader : IFloodZoneDataLoader
{
    private static readonly WKBReader WkbReader = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<FloodZone> LoadZones(FloodDataOptions options, CancellationToken cancellationToken = default)
    {
        var extentsPath = Path.Combine(options.DataRoot, options.ExtentsFile);

        if (!File.Exists(extentsPath))
        {
            return [];
        }

        var zones = new List<FloodZone>();

        foreach (var line in File.ReadLines(extentsPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var record = JsonSerializer.Deserialize<FloodZoneRecord>(line, JsonOptions);
            if (record is null)
                continue;

            var wkb = Convert.FromBase64String(record.PolygonWkbBase64);
            var geometry = WkbReader.Read(wkb);

            var risk = Enum.TryParse<FloodRisk>(record.Risk, ignoreCase: true, out var parsed)
                ? parsed
                : FloodRisk.Unknown;

            zones.Add(new FloodZone
            {
                Id = record.Id,
                Risk = risk,
                Geometry = geometry
            });
        }

        return zones;
    }

    private sealed record FloodZoneRecord
    {
        public required string Id { get; init; }
        public required string Risk { get; init; }
        public required string PolygonWkbBase64 { get; init; }
    }
}
