namespace AmmonsDataLabs.BuyersAgent.Geo;

public sealed class FileGeocodingOptions
{
    public const string SectionName = "FileGeocoding";

    public string FilePath { get; init; } = string.Empty;
}
