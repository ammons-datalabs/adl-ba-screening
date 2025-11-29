namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

/// <summary>
/// Deterministic stub for flood screening with pattern-based risk assignment.
/// - Addresses containing "Main Road" -> High risk
/// - Addresses containing "Street" -> Low risk
/// - All other addresses -> Unknown risk
/// </summary>
public class StubFloodScreeningService : IFloodScreeningService
{
    public Task<FloodLookupResponse> ScreenAsync(FloodLookupRequest request, CancellationToken cancellationToken)
    {
        var results = request.Properties.Select(p => new FloodLookupResult
        {
            Address = p.Address,
            Risk = DetermineRisk(p.Address),
            Reasons = [GetReasonForRisk(p.Address)]
        }).ToList();

        return Task.FromResult(new FloodLookupResponse { Results = results });
    }

    private static FloodRisk DetermineRisk(string address)
    {
        if (address.Contains("Main Road", StringComparison.OrdinalIgnoreCase))
            return FloodRisk.High;

        if (address.Contains("Street", StringComparison.OrdinalIgnoreCase))
            return FloodRisk.Low;

        return FloodRisk.Unknown;
    }

    private static string GetReasonForRisk(string address)
    {
        var risk = DetermineRisk(address);
        return risk switch
        {
            FloodRisk.High => "Stub: Address on Main Road - high flood risk zone.",
            FloodRisk.Low => "Stub: Address on Street - low flood risk zone.",
            _ => "Stub: Unable to determine flood risk for this address."
        };
    }
}