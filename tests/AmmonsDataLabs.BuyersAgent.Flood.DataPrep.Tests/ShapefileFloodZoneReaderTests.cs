using AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests.Helpers;
using NetTopologySuite.Geometries;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests;

public class ShapefileFloodZoneReaderTests : IDisposable
{
    private readonly string _shpPath;
    private readonly string _tempDir;

    public ShapefileFloodZoneReaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"flood-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _shpPath = Path.Combine(_tempDir, "sample-bcc-flood");

        SyntheticShapefileGenerator.GenerateBccFloodShapefile(_shpPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Reader_ConvertsFeaturesToFloodZones()
    {
        static (string id, FloodRisk risk) MapAttributes(IDictionary<string, object?> attributes)
        {
            var id = attributes["OBJECTID"]?.ToString() ?? "unknown";
            var likelihoodString = attributes.TryGetValue("LIKELIHOOD", out var raw) ? raw?.ToString() : null;
            var risk = FloodLikelihoodMapper.Map(likelihoodString);
            return (id, risk);
        }

        var zones = ShapefileFloodZoneReader.Read(_shpPath + ".shp", MapAttributes).ToList();

        Assert.Equal(3, zones.Count);

        var highRisk = zones.First(z => z.Risk == FloodRisk.High);
        Assert.Equal("1", highRisk.Id);
        Assert.IsAssignableFrom<Polygon>(highRisk.Geometry);

        var mediumRisk = zones.First(z => z.Risk == FloodRisk.Medium);
        Assert.Equal("2", mediumRisk.Id);

        var lowRisk = zones.First(z => z.Risk == FloodRisk.Low);
        Assert.Equal("3", lowRisk.Id);
    }

    [Fact]
    public void Reader_PreservesGeometryCoordinates()
    {
        static (string id, FloodRisk risk) MapAttributes(IDictionary<string, object?> attributes)
        {
            var id = attributes["OBJECTID"]?.ToString() ?? "unknown";
            var likelihoodString = attributes.TryGetValue("LIKELIHOOD", out var raw) ? raw?.ToString() : null;
            var risk = FloodLikelihoodMapper.Map(likelihoodString);
            return (id, risk);
        }

        var zones = ShapefileFloodZoneReader.Read(_shpPath + ".shp", MapAttributes).ToList();

        var highRiskZone = zones.First(z => z.Risk == FloodRisk.High);
        var envelope = highRiskZone.Geometry.EnvelopeInternal;

        // Check bounds are approximately correct (the High zone coordinates)
        Assert.True(envelope.MinX >= 153.00 - 0.01 && envelope.MinX <= 153.00 + 0.01);
        Assert.True(envelope.MaxX >= 153.05 - 0.01 && envelope.MaxX <= 153.05 + 0.01);
    }
}