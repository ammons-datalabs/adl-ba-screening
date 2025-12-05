namespace AmmonsDataLabs.BuyersAgent.Geo;

public sealed class GeocodingOptions
{
    public const string SectionName = "Geocoding";

    /// <summary>
    /// The geocoding provider to use. Options: Dummy, File, AzureMaps
    /// </summary>
    public string Provider { get; init; } = "Dummy";
}