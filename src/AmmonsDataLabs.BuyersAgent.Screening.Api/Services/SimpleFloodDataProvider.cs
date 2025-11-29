using AmmonsDataLabs.BuyersAgent.Flood;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Services;

/// <summary>
/// Simple flood data provider for demo/development
/// Uses address pattern matching as a crude proxy for real flood data.
/// Will be replaced by QldFloodDataProvider when real data is wired up.
/// </summary>
public class SimpleFloodDataProvider : IFloodDataProvider
{
    // Patters suggesting proximity to flood-prone infrastructure
    private static readonly string[] HighRiskPatterns =
        ["Main Rd", "Main Road", "Motorway", "Highway"];
    
    // Patterns suggesting proximity to waterways
    private static readonly string[] MediumRiskPatterns =
        ["Ck", "Creek", "River"];

    public Task<FloodLookupResult> LookupAsync(string address, CancellationToken cancellationToken = default)
    {
        var result = EvaluateAddress(address);
        return Task.FromResult(result);
    }

    private static FloodLookupResult EvaluateAddress(string address)
    {
        // Check high-risk patterns first
        if (ContainsAny(address, HighRiskPatterns))
        {
            return new FloodLookupResult
            {
                Address = address,
                Risk = FloodRisk.High,
                Reasons = ["Near major road (demo rule - pending real flood data)."]
            };
        }
        
        // Check medium-risk patterns
        if (ContainsAny(address, MediumRiskPatterns))
        {
            return new FloodLookupResult
            {
                Address = address,
                Risk =  FloodRisk.Medium,
                Reasons = ["Near waterway (demo rule - pending real flood data)."]
            };
        }
        
        // Default
        return new FloodLookupResult
        {
            Address = address,
            Risk = FloodRisk.Low,
            Reasons = ["No known flood indicators (demo rule - pending real flood data.)"]
        };
    }

    private static bool ContainsAny(string address, string[] patterns)
    {
        return patterns.Any(p =>
            address.Contains(p, StringComparison.OrdinalIgnoreCase));
    }
}