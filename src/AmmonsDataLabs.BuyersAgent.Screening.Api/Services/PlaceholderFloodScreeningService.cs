using AmmonsDataLabs.BuyersAgent.Flood;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Services;

public sealed class PlaceholderFloodScreeningService : IFloodScreeningService
{
    public Task<FloodLookupResponse> ScreenAsync(
        FloodLookupRequest request,
        CancellationToken ct = default)
    {
        var results = request.Properties.Select(p => new FloodLookupResult
        {
            Address = p.Address,
            Risk = FloodRisk.Unknown,
            Reasons = ["Flood screening not yet implemented."]
        }).ToList();

        var floodLookupResponse = new FloodLookupResponse { Results = results };
        return Task.FromResult(floodLookupResponse);
    }
}