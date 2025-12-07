using AmmonsDataLabs.BuyersAgent.Screening.Api.Models;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Services;

public interface IFloodAnomalyStore
{
    Task AddAsync(FloodAnomalyReport report, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FloodAnomalyReport>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
