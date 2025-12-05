using AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests.Helpers;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests;

public class GeoJsonFloodZoneReaderTests : IDisposable
{
    private readonly string _extentsGeoJsonPath;
    private readonly string _riskGeoJsonPath;
    private readonly string _tempDir;

    public GeoJsonFloodZoneReaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"flood-geojson-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        _riskGeoJsonPath = Path.Combine(_tempDir, "flood-risk.geojson");
        _extentsGeoJsonPath = Path.Combine(_tempDir, "flood-extents.geojson");

        SyntheticGeoJsonGenerator.GenerateBccFloodRiskGeoJson(_riskGeoJsonPath);
        SyntheticGeoJsonGenerator.GenerateBccExtentsGeoJson(_extentsGeoJsonPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Reader_ConvertsRiskFeaturesToFloodZones()
    {
        static (string id, FloodRisk risk) MapAttributes(IAttributesTable attributes)
        {
            var id = attributes["objectid"]?.ToString() ?? "unknown";
            var floodRisk = attributes["flood_risk"]?.ToString();
            var risk = FloodLikelihoodMapper.Map(floodRisk);
            return (id, risk);
        }

        var zones = GeoJsonFloodZoneReader.Read(_riskGeoJsonPath, MapAttributes).ToList();

        Assert.Equal(5, zones.Count);

        var highRisk = zones.First(z => z.Risk == FloodRisk.High && z.Id == "1");
        Assert.IsAssignableFrom<Polygon>(highRisk.Geometry);

        var mediumRisk = zones.First(z => z.Risk == FloodRisk.Medium);
        Assert.Equal("2", mediumRisk.Id);

        var lowRisk = zones.First(z => z.Risk == FloodRisk.Low && z.Id == "3");
        Assert.Equal("3", lowRisk.Id);

        // Extreme maps to High
        var extremeAsHigh = zones.First(z => z.Id == "5");
        Assert.Equal(FloodRisk.High, extremeAsHigh.Risk);

        // Very Low maps to Low
        var veryLowAsLow = zones.First(z => z.Id == "4");
        Assert.Equal(FloodRisk.Low, veryLowAsLow.Risk);
    }

    [Fact]
    public void Reader_ConvertsExtentsFeaturesToFloodZones()
    {
        static (string id, FloodRisk risk) MapAttributes(IAttributesTable attributes)
        {
            var id = attributes["objectid"]?.ToString() ?? "unknown";
            // Extents have no risk level
            return (id, FloodRisk.Unknown);
        }

        var zones = GeoJsonFloodZoneReader.Read(_extentsGeoJsonPath, MapAttributes).ToList();

        Assert.Equal(2, zones.Count);
        Assert.All(zones, z => Assert.Equal(FloodRisk.Unknown, z.Risk));
        Assert.Contains(zones, z => z.Id == "101");
        Assert.Contains(zones, z => z.Id == "102");
    }

    [Fact]
    public void Reader_PreservesGeometryCoordinates()
    {
        static (string id, FloodRisk risk) MapAttributes(IAttributesTable attributes)
        {
            var id = attributes["objectid"]?.ToString() ?? "unknown";
            var floodRisk = attributes["flood_risk"]?.ToString();
            var risk = FloodLikelihoodMapper.Map(floodRisk);
            return (id, risk);
        }

        var zones = GeoJsonFloodZoneReader.Read(_riskGeoJsonPath, MapAttributes).ToList();

        var highRiskZone = zones.First(z => z.Id == "1");
        var envelope = highRiskZone.Geometry.EnvelopeInternal;

        // Check bounds are approximately correct (the High zone coordinates from generator)
        // ReSharper disable once MergeIntoPattern
        Assert.True(envelope.MinX >= 153.00 - 0.01 && envelope.MinX <= 153.00 + 0.01);
        // ReSharper disable once MergeIntoPattern
        Assert.True(envelope.MaxX >= 153.05 - 0.01 && envelope.MaxX <= 153.05 + 0.01);
    }
}