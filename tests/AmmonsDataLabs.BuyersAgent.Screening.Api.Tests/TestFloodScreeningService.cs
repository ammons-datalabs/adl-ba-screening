using AmmonsDataLabs.BuyersAgent.Flood;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

public sealed class TestFloodScreeningService : IFloodScreeningService
{
    public Task<FloodLookupResponse> ScreenAsync(
        FloodLookupRequest request, CancellationToken cancellationToken = default)
    {
        var results = request.Properties.Select(p => new FloodLookupResult
        {
            Address = p.Address,
            Risk = FloodRisk.Unknown,
            Reasons = ["Test stub: Flood screening not implemented yet."]
        }).ToList();
        
        return Task.FromResult(new FloodLookupResponse{ Results =  results });
    }
}