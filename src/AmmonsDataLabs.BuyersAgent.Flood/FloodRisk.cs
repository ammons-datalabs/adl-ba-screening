namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Flood risk classification for a property location.
/// </summary>
public enum FloodRisk
{
    /// <summary>
    /// Risk could not be determined (geocoding failed, data unavailable, etc.)
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Location successfully checked and is not in or near any flood zone.
    /// </summary>
    None,

    /// <summary>
    /// Low likelihood flood zone (1% AEP or less frequent).
    /// </summary>
    Low,

    /// <summary>
    /// Medium likelihood flood zone (2-5% AEP).
    /// </summary>
    Medium,

    /// <summary>
    /// High likelihood flood zone (greater than 5% AEP).
    /// </summary>
    High
}