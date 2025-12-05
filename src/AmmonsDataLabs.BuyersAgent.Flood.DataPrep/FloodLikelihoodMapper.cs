namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep;

public static class FloodLikelihoodMapper
{
    public static FloodRisk Map(string? likelihood)
    {
        if (string.IsNullOrWhiteSpace(likelihood))
            return FloodRisk.Unknown;

        return likelihood.Trim().ToLowerInvariant() switch
        {
            "extreme" => FloodRisk.High, // Map Extreme to High (our highest level)
            "high" => FloodRisk.High,
            "medium" => FloodRisk.Medium,
            "low" => FloodRisk.Low,
            "very low" => FloodRisk.Low, // Map Very Low to Low (our lowest level)
            _ => FloodRisk.Unknown
        };
    }
}