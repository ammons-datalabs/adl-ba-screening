using AmmonsDataLabs.BuyersAgent.Geo;

namespace AmmonsDataLabs.BuyersAgent.Flood;

public sealed class InMemoryFloodZoneIndex(IEnumerable<FloodZone> zones) : IFloodZoneIndex
{
    private const double MetresPerDegreeLat = 111_320.0;
    private const double BrisbaneLatRadians = -27.47 * Math.PI / 180.0;
    private static readonly double MetresPerDegreeLon = MetresPerDegreeLat * Math.Cos(BrisbaneLatRadians);

    private readonly List<FloodZone> _zones = zones.ToList();

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

            if (best is null || zone.Risk > best.Risk) best = zone;
        }

        return best;
    }

    public FloodZoneHit? FindNearestZone(GeoPoint point, double maxDistanceMetres)
    {
        if (_zones.Count == 0)
            return null;

        var ntsPoint = GeoFactory.CreatePoint(point);
        FloodZoneHit? bestHit = null;

        foreach (var zone in _zones)
        {
            if (zone.Geometry.Contains(ntsPoint))
            {
                if (bestHit is null ||
                    bestHit.Proximity != FloodZoneProximity.Inside ||
                    zone.Risk > bestHit.Zone.Risk)
                    bestHit = new FloodZoneHit
                    {
                        Zone = zone,
                        DistanceMetres = 0,
                        Proximity = FloodZoneProximity.Inside
                    };
                continue;
            }

            var degreeDistance = zone.Geometry.Distance(ntsPoint);
            var metreDistance = DegreesToMetres(degreeDistance);

            if (metreDistance > maxDistanceMetres)
                continue;

            if (bestHit is null)
                bestHit = new FloodZoneHit
                {
                    Zone = zone,
                    DistanceMetres = metreDistance,
                    Proximity = FloodZoneProximity.Near
                };
            else if (bestHit.Proximity == FloodZoneProximity.Near)
                if (zone.Risk > bestHit.Zone.Risk ||
                    (zone.Risk == bestHit.Zone.Risk && metreDistance < bestHit.DistanceMetres))
                    bestHit = new FloodZoneHit
                    {
                        Zone = zone,
                        DistanceMetres = metreDistance,
                        Proximity = FloodZoneProximity.Near
                    };
        }

        return bestHit;
    }

    private static double DegreesToMetres(double degreeDistance)
    {
        return degreeDistance * MetresPerDegreeLat;
    }
}