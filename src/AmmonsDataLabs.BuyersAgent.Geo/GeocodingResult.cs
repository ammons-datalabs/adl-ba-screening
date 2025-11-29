namespace AmmonsDataLabs.BuyersAgent.Geo;

public sealed class GeocodingResult
{
    public required string Query { get; init; }
    public string? NormalizedAddress { get; init; }
    public GeoPoint? Location { get; init; }
    public GeocodingStatus Status { get; init; }
    public string? Provider { get; init; }
}
