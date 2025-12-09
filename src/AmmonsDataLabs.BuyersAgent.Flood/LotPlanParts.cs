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
    /// Supports both numeric lots (e.g., "3GTP102995") and letter lots (e.g., "ASP279272").
    /// Letter lots are used for strata/community title properties (A = common property, B-Z = various allocations).
    /// </summary>
    /// <param name="lotplan">The lotplan string (e.g., "3GTP102995" or "ASP279272")</param>
    /// <returns>A LotPlanParts containing the lot identifier and plan identifier</returns>
    /// <exception cref="ArgumentException">If lotplan is null or whitespace</exception>
    /// <exception cref="FormatException">If lotplan doesn't match expected format</exception>
    public static LotPlanParts Parse(string lotplan)
    {
        if (string.IsNullOrWhiteSpace(lotplan))
            throw new ArgumentException("lotplan cannot be null or empty", nameof(lotplan));

        // Try numeric lot first (e.g., "101SP279272" -> Lot="101", Plan="SP279272")
        var i = 0;
        while (i < lotplan.Length && char.IsDigit(lotplan[i]))
            i++;

        if (i > 0 && i < lotplan.Length)
        {
            var lot = lotplan[..i];
            var plan = lotplan[i..];
            return new LotPlanParts(lot, plan);
        }

        // Try letter lot (e.g., "ASP279272" -> Lot="A", Plan="SP279272")
        // Letter lots are used for strata/community title properties.
        // The lot is typically a single letter (A-Z) followed by a plan type and number.
        // Important: We must ensure the string doesn't start with a plan type (no lot at all).
        if (char.IsLetter(lotplan[0]) && char.IsUpper(lotplan[0]))
        {
            // First check: if the entire string is a valid plan (plan type + digits), it has no lot - reject it
            if (IsValidPlan(lotplan))
                throw new FormatException($"Invalid lotplan format: '{lotplan}'");

            // Find where the plan type starts by looking for a known plan type followed by digits
            for (var j = 1; j < lotplan.Length - 1; j++)
            {
                var potentialPlan = lotplan[j..];
                if (IsValidPlan(potentialPlan))
                {
                    var lot = lotplan[..j];
                    return new LotPlanParts(lot, potentialPlan);
                }
            }
        }

        throw new FormatException($"Invalid lotplan format: '{lotplan}'");
    }

    /// <summary>
    /// Checks if a string is a valid plan identifier (plan type followed by digits).
    /// Plan types from Queensland data: SP, RP, BUP, GTP, CP, MPH, SL, USL, AP, MCP, MSP, NPW, PER, RL, SBP, SPS, SRP, SSP, etc.
    /// </summary>
    private static bool IsValidPlan(string s)
    {
        // Plan type prefixes ordered by length (longest first to avoid partial matches)
        string[] planTypes =
        [
            "BUP", "GTP", "MPH", "USL", "MCP", "MSP", "NPW", "PER", "SBP", "SPS", "SRP", "SSP", "MCH",
            "SP", "RP", "CP", "SL", "AP", "RL", "AG", "CC", "BS",
            "B", "C", "K", "M", "N", "P", "S", "V", "W"
        ];

        foreach (var planType in planTypes)
        {
            if (s.StartsWith(planType, StringComparison.Ordinal) &&
                s.Length > planType.Length &&
                char.IsDigit(s[planType.Length]))
            {
                return true;
            }
        }

        return false;
    }
}