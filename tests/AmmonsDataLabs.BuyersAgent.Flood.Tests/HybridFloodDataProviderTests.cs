using AmmonsDataLabs.BuyersAgent.Geo;
using Xunit;

namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class HybridFloodDataProviderTests
{
    [Fact]
    public async Task Lookup_UsesParcelMetrics_WhenAvailable()
    {
        var geocoder = new StubGeocoder("3/241 Horizon Drive, Westlake", "3GTP102995");
        var metricsIndex = new StubMetricsIndex(
            lotPlan: "3GTP102995",
            new BccMetricsSnapshot
            {
                LotPlanOrPlanKey = "3GTP102995",
                Plan = "GTP102995",
                OverallRisk = FloodRisk.High,
                HasFloodInfo = true,
                Scope = MetricsScope.Parcel,
                EvidenceMetrics = ["FL_HIGH_RIVER"]
            });

        var provider = new HybridFloodDataProvider(geocoder, metricsIndex, new StubFloodZoneIndex(null));

        var result = await provider.LookupAsync("3/241 Horizon Drive, Westlake");

        Assert.Equal(FloodRisk.High, result.Risk);
        Assert.Equal(FloodDataSource.BccParcelMetrics, result.Source);
        Assert.Equal(FloodDataScope.Parcel, result.Scope);
    }

    [Fact]
    public async Task Lookup_UsesPlanFallback_WhenParcelMetricsMissing()
    {
        var geocoder = new StubGeocoder("3/241 Horizon Drive, Westlake", "3GTP102995");
        // Index returns metrics via plan fallback, not direct parcel
        var metricsIndex = new StubMetricsIndex(
            lotPlanForDirectHit: null, // no direct parcel hit
            fallbackFor: "3GTP102995",
            new BccMetricsSnapshot
            {
                LotPlanOrPlanKey = "3GTP102995",
                Plan = "GTP102995",
                OverallRisk = FloodRisk.High,
                HasFloodInfo = true,
                Scope = MetricsScope.PlanFallback,
                EvidenceMetrics = ["FL_HIGH_RIVER"]
            });

        var provider = new HybridFloodDataProvider(geocoder, metricsIndex, new StubFloodZoneIndex(null));

        var result = await provider.LookupAsync("3/241 Horizon Drive, Westlake");

        Assert.Equal(FloodRisk.High, result.Risk);
        Assert.Equal(FloodDataSource.BccParcelMetrics, result.Source);
        Assert.Equal(FloodDataScope.PlanFallback, result.Scope);
        Assert.Contains("plan-level", string.Join(" ", result.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Lookup_ReturnsNone_WhenMetricsExistButNoFloodInfo()
    {
        var geocoder = new StubGeocoder("117 Fernberg Rd, Paddington", "1RP84382");
        var metricsIndex = new StubMetricsIndex(
            lotPlan: "1RP84382",
            new BccMetricsSnapshot
            {
                LotPlanOrPlanKey = "1RP84382",
                Plan = "RP84382",
                OverallRisk = FloodRisk.Unknown,
                HasFloodInfo = false, // No flood info in BCC data
                Scope = MetricsScope.Parcel,
                EvidenceMetrics = []
            });

        var provider = new HybridFloodDataProvider(geocoder, metricsIndex, new StubFloodZoneIndex(null));

        var result = await provider.LookupAsync("117 Fernberg Rd, Paddington");

        // Property exists in BCC data but has no flood info = None risk
        Assert.Equal(FloodRisk.None, result.Risk);
        Assert.Equal(FloodDataSource.BccParcelMetrics, result.Source);
    }

    [Fact]
    public async Task Lookup_FallsToTier3_WhenNoLotPlan()
    {
        var geocoder = new StubGeocoder("123 Unknown St", null); // no lotplan from geocoding
        var metricsIndex = new StubMetricsIndex(); // always returns false

        var polygon = GeoFactory.CreatePolygon(
            new GeoPoint(-27.48, 153.00),
            new GeoPoint(-27.48, 153.05),
            new GeoPoint(-27.45, 153.05),
            new GeoPoint(-27.45, 153.00));

        var zone = new FloodZone
        {
            Id = "zone-1",
            Risk = FloodRisk.Medium,
            Geometry = polygon
        };

        var zoneIndex = new StubFloodZoneIndex(zone);

        var provider = new HybridFloodDataProvider(geocoder, metricsIndex, zoneIndex);

        var result = await provider.LookupAsync("123 Unknown St");

        // Falls through to Tier 3 (point buffer) because no lotplan available
        Assert.Equal(FloodRisk.Medium, result.Risk);
        Assert.Equal(FloodDataSource.PointBuffer, result.Source);
    }

    [Fact]
    public async Task Lookup_FallsToTier3_WhenLotPlanNotInMetricsIndex()
    {
        // Geocoding returns a lotplan, but metrics index doesn't have data for it
        var geocoder = new StubGeocoder("456 New Development St", "999XYZ123");
        var metricsIndex = new StubMetricsIndex(); // always returns false - no metrics for this lotplan

        var polygon = GeoFactory.CreatePolygon(
            new GeoPoint(-27.48, 153.00),
            new GeoPoint(-27.48, 153.05),
            new GeoPoint(-27.45, 153.05),
            new GeoPoint(-27.45, 153.00));

        var zone = new FloodZone
        {
            Id = "zone-1",
            Risk = FloodRisk.Low,
            Geometry = polygon
        };

        var zoneIndex = new StubFloodZoneIndex(zone);

        var provider = new HybridFloodDataProvider(geocoder, metricsIndex, zoneIndex);

        var result = await provider.LookupAsync("456 New Development St");

        // Falls through to Tier 3 (point buffer) because lotplan not in metrics index
        Assert.Equal(FloodRisk.Low, result.Risk);
        Assert.Equal(FloodDataSource.PointBuffer, result.Source);
    }

    [Fact]
    public async Task Lookup_EmptyAddress_ReturnsUnknown()
    {
        var geocoder = new StubGeocoder("", null);
        var metricsIndex = new StubMetricsIndex();
        var zoneIndex = new StubFloodZoneIndex(null);

        var provider = new HybridFloodDataProvider(geocoder, metricsIndex, zoneIndex);

        var result = await provider.LookupAsync("   ");

        Assert.Equal(FloodRisk.Unknown, result.Risk);
        Assert.Contains("empty", string.Join(" ", result.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Lookup_GeocodingFails_ReturnsUnknown()
    {
        var geocoder = new FailingGeocoder();
        var metricsIndex = new StubMetricsIndex();
        var zoneIndex = new StubFloodZoneIndex(null);

        var provider = new HybridFloodDataProvider(geocoder, metricsIndex, zoneIndex);

        var result = await provider.LookupAsync("999 Nowhere St");

        Assert.Equal(FloodRisk.Unknown, result.Risk);
    }

    // Test stub implementations
    private sealed class StubGeocoder : IGeocodingService
    {
        private readonly string _expectedAddress;
        private readonly string? _lotPlan;

        public StubGeocoder(string expectedAddress, string? lotPlan)
        {
            _expectedAddress = expectedAddress;
            _lotPlan = lotPlan;
        }

        public Task<GeocodingResult> GeocodeAsync(string address, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GeocodingResult
            {
                Query = address,
                NormalizedAddress = _expectedAddress,
                Location = new GeoPoint(-27.46, 153.02),
                Status = GeocodingStatus.Success,
                Provider = "StubGeocoder",
                LotPlan = _lotPlan
            });
        }
    }

    private sealed class FailingGeocoder : IGeocodingService
    {
        public Task<GeocodingResult> GeocodeAsync(string address, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GeocodingResult
            {
                Query = address,
                Status = GeocodingStatus.NotFound,
                Provider = "FailingGeocoder"
            });
        }
    }

    private sealed class StubMetricsIndex : IBccParcelMetricsIndex
    {
        private readonly string? _lotPlanForDirectHit;
        private readonly string? _fallbackFor;
        private readonly BccMetricsSnapshot? _snapshot;

        public StubMetricsIndex()
        {
            _snapshot = null;
        }

        public StubMetricsIndex(string lotPlan, BccMetricsSnapshot snapshot)
        {
            _lotPlanForDirectHit = lotPlan;
            _fallbackFor = lotPlan;
            _snapshot = snapshot;
        }

        public StubMetricsIndex(string? lotPlanForDirectHit, string? fallbackFor, BccMetricsSnapshot snapshot)
        {
            _lotPlanForDirectHit = lotPlanForDirectHit;
            _fallbackFor = fallbackFor;
            _snapshot = snapshot;
        }

        public bool TryGet(string lotPlan, out BccMetricsSnapshot metrics)
        {
            // Check direct hit
            if (_lotPlanForDirectHit != null &&
                string.Equals(lotPlan, _lotPlanForDirectHit, StringComparison.OrdinalIgnoreCase))
            {
                metrics = _snapshot!;
                return true;
            }

            // Check fallback
            if (_fallbackFor != null &&
                string.Equals(lotPlan, _fallbackFor, StringComparison.OrdinalIgnoreCase) &&
                _snapshot != null)
            {
                metrics = _snapshot;
                return true;
            }

            metrics = default!;
            return false;
        }
    }

    private sealed class StubFloodZoneIndex : IFloodZoneIndex
    {
        private readonly FloodZone? _zone;

        public StubFloodZoneIndex(FloodZone? zone) => _zone = zone;

        public FloodZone? FindZoneForPoint(GeoPoint point) => _zone;

        public FloodZoneHit? FindNearestZone(GeoPoint point, double maxDistanceMetres)
        {
            if (_zone is null)
                return null;

            return new FloodZoneHit
            {
                Zone = _zone,
                DistanceMetres = 0,
                Proximity = FloodZoneProximity.Inside
            };
        }
    }
}
