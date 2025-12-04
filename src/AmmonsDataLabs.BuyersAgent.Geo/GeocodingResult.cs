namespace AmmonsDataLabs.BuyersAgent.Geo;

public sealed class GeocodingResult
{
    public required string Query { get; init; }
    public string? NormalizedAddress { get; init; }
    public GeoPoint? Location { get; init; }
    public GeocodingStatus Status { get; init; }
    public string? Provider { get; init; }

    /// <summary>
    /// Queensland lotplan identifier if available (e.g., "3GTP102995").
    /// Used for parcel-level flood metrics lookup.
    /// </summary>
    public string? LotPlan { get; init; }
}
