namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Reverse geocoding service that finds a Queensland lot plan identifier
/// for a given coordinate. Used when the primary geocoder (e.g., Azure Maps)
/// returns lat/lon but no lot plan.
/// </summary>
public interface ILotPlanLookup
{
    /// <summary>
    /// Finds the lot plan for the parcel nearest to the given coordinates.
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees (WGS84).</param>
    /// <param name="longitude">Longitude in decimal degrees (WGS84).</param>
    /// <param name="maxDistanceMetres">Maximum distance to search (default 40m).</param>
    /// <returns>The lot plan identifier (e.g., "18RP67505") or null if none found within range.</returns>
    string? FindLotPlan(double latitude, double longitude, double maxDistanceMetres = 40.0);
}
