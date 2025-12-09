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
        return LoadZonesFromFile(extentsPath, cancellationToken);
    }

    public IReadOnlyList<FloodZone> LoadRiskZones(FloodDataOptions options, CancellationToken cancellationToken = default)
    {
        var riskPath = Path.Combine(options.DataRoot, options.OverallRiskFile);
        return LoadZonesFromFile(riskPath, cancellationToken);
    }

    private static IReadOnlyList<FloodZone> LoadZonesFromFile(string filePath, CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath)) return [];

        var zones = new List<FloodZone>();

        foreach (var line in File.ReadLines(filePath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var record = JsonSerializer.Deserialize<FloodZoneRecord>(line, JsonOptions);
            if (record is null)
                continue;

            var wkb = Convert.FromBase64String(record.PolygonWkbBase64);
            var geometry = WkbReader.Read(wkb);

            var risk = Enum.TryParse<FloodRisk>(record.Risk, true, out var parsed)
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