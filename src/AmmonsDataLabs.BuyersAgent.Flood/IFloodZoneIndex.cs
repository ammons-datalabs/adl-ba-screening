using AmmonsDataLabs.BuyersAgent.Geo;

namespace AmmonsDataLabs.BuyersAgent.Flood;

public interface IFloodZoneIndex
{
    FloodZone? FindZoneForPoint(GeoPoint point);

    FloodZoneHit? FindNearestZone(GeoPoint point, double maxDistanceMetres);

    /// <summary>
    /// Finds the classified flood risk for a point by checking against the flood-risk overlay polygons.
    /// Used as a fallback when the main extent lookup returns Unknown risk.
    /// </summary>
    /// <param name="point">The geographic point to check</param>
    /// <returns>The classified flood risk, or null if no risk zone contains the point</returns>
    FloodRisk? FindRiskOverlayForPoint(GeoPoint point);
}