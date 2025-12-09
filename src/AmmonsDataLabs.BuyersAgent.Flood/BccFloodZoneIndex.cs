using AmmonsDataLabs.BuyersAgent.Flood.Configuration;
using AmmonsDataLabs.BuyersAgent.Geo;
using Microsoft.Extensions.Options;

namespace AmmonsDataLabs.BuyersAgent.Flood;

public sealed class BccFloodZoneIndex(
    IFloodZoneDataLoader loader,
    IOptions<FloodDataOptions> options)
    : IFloodZoneIndex
{
    private readonly IFloodZoneDataLoader _loader = loader ?? throw new ArgumentNullException(nameof(loader));
    private readonly FloodDataOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly object _extentsSync = new();
    private readonly object _riskSync = new();
    private InMemoryFloodZoneIndex? _extentsIndex;
    private InMemoryFloodZoneIndex? _riskIndex;

    public FloodZone? FindZoneForPoint(GeoPoint point)
    {
        EnsureExtentsLoaded();
        return _extentsIndex!.FindZoneForPoint(point);
    }

    public FloodZoneHit? FindNearestZone(GeoPoint point, double maxDistanceMetres)
    {
        EnsureExtentsLoaded();
        return _extentsIndex!.FindNearestZone(point, maxDistanceMetres);
    }

    public FloodRisk? FindRiskOverlayForPoint(GeoPoint point)
    {
        EnsureRiskLoaded();
        var zone = _riskIndex!.FindZoneForPoint(point);
        return zone?.Risk;
    }

    private void EnsureExtentsLoaded()
    {
        if (_extentsIndex is not null)
            return;

        lock (_extentsSync)
        {
            if (_extentsIndex is not null)
                return;

            var zones = _loader.LoadZones(_options, CancellationToken.None);
            _extentsIndex = new InMemoryFloodZoneIndex(zones);
        }
    }

    private void EnsureRiskLoaded()
    {
        if (_riskIndex is not null)
            return;

        lock (_riskSync)
        {
            if (_riskIndex is not null)
                return;

            var zones = _loader.LoadRiskZones(_options, CancellationToken.None);
            _riskIndex = new InMemoryFloodZoneIndex(zones);
        }
    }
}