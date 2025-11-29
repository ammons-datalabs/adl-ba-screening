using AmmonsDataLabs.BuyersAgent.Geo;

namespace AmmonsDataLabs.BuyersAgent.Flood;

public sealed class InMemoryFloodZoneIndex : IFloodZoneIndex
{
    private readonly List<FloodZone> _zones;

    public InMemoryFloodZoneIndex(IEnumerable<FloodZone> zones)
    {
        _zones = zones.ToList();
    }

    public FloodZone? FindZoneForPoint(GeoPoint point)
    {
        if (_zones.Count == 0)
            return null;

        var ntsPoint = GeoFactory.CreatePoint(point);
        FloodZone? best = null;

        foreach (var zone in _zones)
        {
            if (!zone.Geometry.Contains(ntsPoint))
                continue;

            if (best is null || zone.Risk > best.Risk)
            {
                best = zone;
            }
        }

        return best;
    }
}
