using AmmonsDataLabs.BuyersAgent.Geo;

namespace AmmonsDataLabs.BuyersAgent.Flood;

public interface IFloodZoneIndex
{
    FloodZone? FindZoneForPoint(GeoPoint point);

    FloodZoneHit? FindNearestZone(GeoPoint point, double maxDistanceMetres);
}
