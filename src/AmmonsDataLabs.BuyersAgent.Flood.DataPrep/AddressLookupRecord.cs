namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep;

/// <summary>
/// Represents an address-to-lotplan lookup entry.
/// </summary>
public sealed class AddressLookupRecord
{
    /// <summary>
    /// The Queensland lotplan identifier (e.g., "3GTP102995").
    /// </summary>
    public required string LotPlan { get; init; }

    /// <summary>
    /// The plan portion of the lotplan (e.g., "GTP102995").
    /// </summary>
    public required string Plan { get; init; }

    /// <summary>
    /// Unit number if applicable (e.g., "3" for "3/241 Horizon Drive").
    /// </summary>
    public string? UnitNumber { get; init; }

    /// <summary>
    /// House/street number (e.g., "241").
    /// </summary>
    public string? HouseNumber { get; init; }

    /// <summary>
    /// House number suffix if any (e.g., "A" in "241A").
    /// </summary>
    public string? HouseNumberSuffix { get; init; }

    /// <summary>
    /// Street/corridor name without suffix (e.g., "HORIZON").
    /// </summary>
    public string? CorridorName { get; init; }

    /// <summary>
    /// Street suffix code (e.g., "DR" for Drive, "RD" for Road).
    /// </summary>
    public string? CorridorSuffixCode { get; init; }

    /// <summary>
    /// Suburb name.
    /// </summary>
    public string? Suburb { get; init; }

    /// <summary>
    /// Postcode.
    /// </summary>
    public string? Postcode { get; init; }

    /// <summary>
    /// Latitude of parcel centroid.
    /// </summary>
    public double? Latitude { get; init; }

    /// <summary>
    /// Longitude of parcel centroid.
    /// </summary>
    public double? Longitude { get; init; }

    /// <summary>
    /// Normalized full address string for display.
    /// </summary>
    public string? NormalizedAddress { get; init; }
}