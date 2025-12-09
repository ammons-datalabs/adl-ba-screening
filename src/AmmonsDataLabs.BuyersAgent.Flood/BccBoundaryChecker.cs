using AmmonsDataLabs.BuyersAgent.Geo;

namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Simple bounding box check for Brisbane City Council LGA.
/// Uses approximate bounds that encompass the entire BCC area.
/// </summary>
public static class BccBoundaryChecker
{
    // BCC LGA approximate bounding box (slightly generous to avoid false positives)
    // Based on BCC boundaries from QSpatial data
    private const double MinLatitude = -27.70;   // South (near Redland Bay border)
    private const double MaxLatitude = -27.00;   // North (near Moreton Bay)
    private const double MinLongitude = 152.65;  // West (near Ipswich border)
    private const double MaxLongitude = 153.20;  // East (near bay)

    /// <summary>
    /// Checks if a point is within the BCC LGA bounding box.
    /// This is a rough check - points near the boundary may be incorrectly classified.
    /// </summary>
    public static bool IsInsideBccBounds(GeoPoint point)
    {
        return point.Latitude is >= MinLatitude and <= MaxLatitude &&
               point.Longitude is >= MinLongitude and <= MaxLongitude;
    }
}
