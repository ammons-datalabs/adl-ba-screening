namespace AmmonsDataLabs.BuyersAgent.Geo;

public sealed class AzureMapsOptions
{
    public const string SectionName = "AzureMaps";

    public string SubscriptionKey { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://atlas.microsoft.com";
}
