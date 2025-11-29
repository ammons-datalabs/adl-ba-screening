using AmmonsDataLabs.BuyersAgent.Flood;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

/// <summary>
/// Deterministic flood data provider for integration testing
/// - Addresses containing "Main Road" -> High risk
/// - Addresses containing "Street" -> Low risk  
/// - All other addresses -> Unknown risk
/// </summary>
public class StubFloodDataProvider : IFloodDataProvider
{
    public Task<FloodLookupResult> LookupAsync(string address, CancellationToken cancellationToken = default)
    {
        var result = EvaluateAddress(address);
        return Task.FromResult(result);
    }

    private static FloodLookupResult EvaluateAddress(string address)
    {
        if (address.Contains("Main Road", StringComparison.OrdinalIgnoreCase))
        {
            return new FloodLookupResult
            {
                Address = address,
                Risk = FloodRisk.High,
                Reasons = ["Stub: High flood risk zone."]
            };
        }

        if (address.Contains("Street", StringComparison.OrdinalIgnoreCase))
        {
            return new FloodLookupResult
            {
                Address = address,
                Risk = FloodRisk.Low,
                Reasons = ["Stub: Low flood risk zone."]
            };
        }

        return new FloodLookupResult
        {
            Address = address,
            Risk = FloodRisk.Unknown,
            Reasons = ["Stub: Unknown flood risk zone."]
        };
    }
}