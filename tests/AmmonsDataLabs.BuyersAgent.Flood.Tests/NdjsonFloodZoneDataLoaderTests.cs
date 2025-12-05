using System.Text;
using System.Text.Json;
using AmmonsDataLabs.BuyersAgent.Flood.Configuration;
using AmmonsDataLabs.BuyersAgent.Geo;
using NetTopologySuite.IO;

namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class NdjsonFloodZoneDataLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public NdjsonFloodZoneDataLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"flood-loader-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void LoadZones_ReadsNdjsonFile()
    {
        // Create test NDJSON file
        var extentsPath = Path.Combine(_tempDir, "extents.ndjson");
        CreateTestNdjsonFile(extentsPath);

        var options = new FloodDataOptions
        {
            DataRoot = _tempDir,
            ExtentsFile = "extents.ndjson"
        };

        var loader = new NdjsonFloodZoneDataLoader();

        var zones = loader.LoadZones(options);

        Assert.Equal(2, zones.Count);

        var highZone = zones.First(z => z.Risk == FloodRisk.High);
        Assert.Equal("zone-1", highZone.Id);
        Assert.NotNull(highZone.Geometry);

        var lowZone = zones.First(z => z.Risk == FloodRisk.Low);
        Assert.Equal("zone-2", lowZone.Id);
    }

    [Fact]
    public void LoadZones_ReturnsEmptyWhenFileNotFound()
    {
        var options = new FloodDataOptions
        {
            DataRoot = _tempDir,
            ExtentsFile = "nonexistent.ndjson"
        };

        var loader = new NdjsonFloodZoneDataLoader();

        var zones = loader.LoadZones(options);

        Assert.Empty(zones);
    }

    private void CreateTestNdjsonFile(string path)
    {
        var wkbWriter = new WKBWriter { Strict = false, HandleSRID = true };

        var zone1 = GeoFactory.CreatePolygon(
            new GeoPoint(-27.48, 153.00),
            new GeoPoint(-27.48, 153.05),
            new GeoPoint(-27.45, 153.05),
            new GeoPoint(-27.45, 153.00));

        var zone2 = GeoFactory.CreatePolygon(
            new GeoPoint(-27.50, 153.10),
            new GeoPoint(-27.50, 153.15),
            new GeoPoint(-27.47, 153.15),
            new GeoPoint(-27.47, 153.10));

        var sb = new StringBuilder();
        sb.AppendLine(JsonSerializer.Serialize(new
        {
            id = "zone-1",
            risk = "High",
            polygonWkbBase64 = Convert.ToBase64String(wkbWriter.Write(zone1))
        }));
        sb.AppendLine(JsonSerializer.Serialize(new
        {
            id = "zone-2",
            risk = "Low",
            polygonWkbBase64 = Convert.ToBase64String(wkbWriter.Write(zone2))
        }));

        File.WriteAllText(path, sb.ToString());
    }
}