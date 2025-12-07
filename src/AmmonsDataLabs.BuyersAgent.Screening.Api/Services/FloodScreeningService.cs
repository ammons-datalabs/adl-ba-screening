using AmmonsDataLabs.BuyersAgent.Flood;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Services;

/// <summary>
/// Flood screening service that delegates to an IFloodDataProvider
/// for the actual flood risk lookup.
/// </summary>
public sealed class FloodScreeningService(IFloodDataProvider dataProvider) : IFloodScreeningService
{
    public async Task<FloodLookupResponse> ScreenAsync(
        FloodLookupRequest request, CancellationToken cancellationToken)
    {
        var results = new List<FloodSummary>();

        foreach (var property in request.Properties)
        {
            var result = await dataProvider.LookupAsync(property.Address, cancellationToken);
            var summary = FloodSummaryMapper.FromResult(result);
            results.Add(summary);
        }

        return new FloodLookupResponse { Results = results };
    }
}