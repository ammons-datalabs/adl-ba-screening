using AmmonsDataLabs.BuyersAgent.Flood;
using AmmonsDataLabs.BuyersAgent.Flood.Configuration;
using AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests.Helpers;
using NetTopologySuite.Geometries;
using Xunit;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests;

public class RealBccDataTests : IDisposable
{
    private readonly string _tempDir;

    public RealBccDataTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"bcc-data-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void NdjsonLoader_ReadsRealBccFloodRiskSample()
    {
        var ndjsonPath = EmbeddedResourceHelper.ExtractToTempFile("sample-flood-risk.ndjson", _tempDir);

        var options = new FloodDataOptions
        {
            DataRoot = _tempDir,
            ExtentsFile = "sample-flood-risk.ndjson"
        };

        var loader = new NdjsonFloodZoneDataLoader();
        var zones = loader.LoadZones(options);

        Assert.Equal(6, zones.Count);

        // Verify risk distribution from real data
        Assert.Equal(2, zones.Count(z => z.Risk == FloodRisk.Low));
        Assert.Equal(2, zones.Count(z => z.Risk == FloodRisk.Medium));
        Assert.Equal(2, zones.Count(z => z.Risk == FloodRisk.High));

        // All zones should have valid geometry
        Assert.All(zones, z => Assert.NotNull(z.Geometry));
        Assert.All(zones, z => Assert.IsAssignableFrom<Polygon>(z.Geometry));

        // Verify zones have Brisbane coordinates (approx 152-154 lon, -27 to -28 lat)
        foreach (var zone in zones)
        {
            var envelope = zone.Geometry.EnvelopeInternal;
            Assert.True(envelope.MinX >= 152 && envelope.MaxX <= 154,
                $"Zone {zone.Id} longitude {envelope.MinX}-{envelope.MaxX} outside Brisbane range");
            Assert.True(envelope.MinY >= -28 && envelope.MaxY <= -27,
                $"Zone {zone.Id} latitude {envelope.MinY}-{envelope.MaxY} outside Brisbane range");
        }
    }

    [Fact]
    public void NdjsonLoader_ReadsRealBccExtentsSample()
    {
        var ndjsonPath = EmbeddedResourceHelper.ExtractToTempFile("sample-flood-extents.ndjson", _tempDir);

        var options = new FloodDataOptions
        {
            DataRoot = _tempDir,
            ExtentsFile = "sample-flood-extents.ndjson"
        };

        var loader = new NdjsonFloodZoneDataLoader();
        var zones = loader.LoadZones(options);

        Assert.Equal(3, zones.Count);

        // Extents have Unknown risk
        Assert.All(zones, z => Assert.Equal(FloodRisk.Unknown, z.Risk));

        // All zones should have valid geometry
        Assert.All(zones, z => Assert.NotNull(z.Geometry));
        Assert.All(zones, z => Assert.IsAssignableFrom<Geometry>(z.Geometry));

        // Verify zones have Brisbane coordinates
        foreach (var zone in zones)
        {
            var envelope = zone.Geometry.EnvelopeInternal;
            Assert.True(envelope.MinX >= 152 && envelope.MaxX <= 154,
                $"Zone {zone.Id} longitude outside Brisbane range");
            Assert.True(envelope.MinY >= -28 && envelope.MaxY <= -27,
                $"Zone {zone.Id} latitude outside Brisbane range");
        }
    }

    [Fact]
    public void NdjsonLoader_PreservesRealBccZoneIds()
    {
        var ndjsonPath = EmbeddedResourceHelper.ExtractToTempFile("sample-flood-risk.ndjson", _tempDir);

        var options = new FloodDataOptions
        {
            DataRoot = _tempDir,
            ExtentsFile = "sample-flood-risk.ndjson"
        };

        var loader = new NdjsonFloodZoneDataLoader();
        var zones = loader.LoadZones(options);

        // Verify real BCC object IDs are preserved
        Assert.All(zones, z => Assert.False(string.IsNullOrEmpty(z.Id)));
        Assert.All(zones, z => Assert.True(int.TryParse(z.Id, out _),
            $"Zone ID '{z.Id}' should be a numeric BCC object ID"));
    }
}
