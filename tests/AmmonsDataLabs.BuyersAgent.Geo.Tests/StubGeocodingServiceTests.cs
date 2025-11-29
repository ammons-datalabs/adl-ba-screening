using AmmonsDataLabs.BuyersAgent.Geo;

namespace AmmonsDataLabs.BuyersAgent.Geo.Tests;

public class StubGeocodingServiceTests
{
    // Test-only stub nested class
    private sealed class StubGeocodingService : IGeocodingService
    {
        public Task<GeocodingResult> GeocodeAsync(string address, CancellationToken cancellationToken = default)
        {
            if (string.Equals(address, "1 Test St, Brisbane QLD", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new GeocodingResult
                {
                    Query = address,
                    NormalizedAddress = "1 Test Street, Brisbane QLD 4000",
                    Location = new GeoPoint(-27.4705, 153.0260),
                    Status = GeocodingStatus.Success,
                    Provider = nameof(StubGeocodingService)
                });
            }

            return Task.FromResult(new GeocodingResult
            {
                Query = address,
                Status = GeocodingStatus.NotFound,
                Provider = nameof(StubGeocodingService)
            });
        }
    }

    private readonly StubGeocodingService _sut = new();

    [Fact]
    public async Task GeocodeAsync_KnownAddress_ReturnsSuccess()
    {
        var result = await _sut.GeocodeAsync("1 Test St, Brisbane QLD", CancellationToken.None);

        Assert.Equal(GeocodingStatus.Success, result.Status);
        Assert.NotNull(result.Location);
    }

    [Fact]
    public async Task GeocodeAsync_UnknownAddress_ReturnsNotFound()
    {
        var result = await _sut.GeocodeAsync("999 Nowhere Rd", CancellationToken.None);

        Assert.Equal(GeocodingStatus.NotFound, result.Status);
        Assert.Null(result.Location);
    }
}
