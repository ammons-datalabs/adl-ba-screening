namespace AmmonsDataLabs.BuyersAgent.Flood;

/// <summary>
/// Represents the parsed components of a Queensland lotplan identifier.
/// Format: {lot_number}{plan_type}{plan_number} e.g., "3GTP102995" -> Lot="3", Plan="GTP102995"
/// </summary>
public readonly record struct LotPlanParts(string Lot, string Plan)
{
    /// <summary>
    /// Gets the common property lot identifier (lot 0) for this plan.
    /// Used as a fallback for multi-lot developments where individual lots may not have metrics.
    /// </summary>
    public string CommonLotPlan => $"0{Plan}";

    /// <summary>
    /// Returns true if this is the common property lot (lot 0).
    /// </summary>
    public bool IsCommonLot => Lot == "0";

    /// <summary>
    /// Parses a lotplan string into its lot and plan components.
    /// </summary>
    /// <param name="lotplan">The lotplan string (e.g., "3GTP102995")</param>
    /// <returns>A LotPlanParts containing the lot number and plan identifier</returns>
    /// <exception cref="ArgumentException">If lotplan is null or whitespace</exception>
    /// <exception cref="FormatException">If lotplan doesn't match expected format</exception>
    public static LotPlanParts Parse(string lotplan)
    {
        if (string.IsNullOrWhiteSpace(lotplan))
            throw new ArgumentException("lotplan cannot be null or empty", nameof(lotplan));

        var i = 0;
        while (i < lotplan.Length && char.IsDigit(lotplan[i]))
            i++;

        if (i == 0 || i == lotplan.Length)
            throw new FormatException($"Invalid lotplan format: '{lotplan}'");

        var lot = lotplan[..i];
        var plan = lotplan[i..];

        return new LotPlanParts(lot, plan);
    }
}