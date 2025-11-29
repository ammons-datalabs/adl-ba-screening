namespace AmmonsDataLabs.BuyersAgent.Flood;

public interface IFloodScreeningService
{
    Task<FloodLookupResponse> ScreenAsync(FloodLookupRequest request, CancellationToken cancellationToken);
}