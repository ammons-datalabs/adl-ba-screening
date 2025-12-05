using AmmonsDataLabs.BuyersAgent.Flood;
using AmmonsDataLabs.BuyersAgent.Screening.Api.Services;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

public class FloodScreeningServiceTests
{
    [Fact]
    public async Task ScreenAsync_DelegatesToProvider_ForEachProperty()
    {
        // Arrange
        var fakeProvider = new FakeFloodDataProvider(FloodRisk.High, "Test reason");
        var service = new FloodScreeningService(fakeProvider);
        var request = new FloodLookupRequest
        {
            Properties =
            [
                new FloodLookupItem { Address = "Address 1" },
                new FloodLookupItem { Address = "Address 2" }
            ]
        };

        // Act
        var response = await service.ScreenAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(2, response.Results.Count);
        Assert.Equal("Address 1", response.Results[0].Address);
        Assert.Equal("Address 2", response.Results[1].Address);
        Assert.All(response.Results, r => Assert.Equal(FloodRisk.High, r.Risk));
    }

    [Fact]
    public async Task ScreenAsync_EmptyProperties_ReturnsEmptyResults()
    {
        // Arrange
        var fakeProvider = new FakeFloodDataProvider(FloodRisk.Low, "N/A");
        var service = new FloodScreeningService(fakeProvider);
        var request = new FloodLookupRequest { Properties = [] };

        // Act
        var response = await service.ScreenAsync(request, CancellationToken.None);

        // Assert
        Assert.Empty(response.Results);
    }


    /// <summary>
    /// Simple fake provider for testing the screening service in isolation
    /// </summary>
    private class FakeFloodDataProvider(FloodRisk risk, string reason) : IFloodDataProvider
    {
        public Task<FloodLookupResult> LookupAsync(string address, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new FloodLookupResult
            {
                Address = address,
                Risk = risk,
                Reasons = [reason]
            });
        }
    }
}