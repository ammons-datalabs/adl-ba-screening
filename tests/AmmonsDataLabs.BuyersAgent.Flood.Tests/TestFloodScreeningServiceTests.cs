namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class TestFloodScreeningServiceTests
{
    private const string FakeStreetBrisbaneQld = "123 Fake Street, Brisbane QLD";
    private const string MainRoadMountGravattQld = "456 Main Road, Mount Gravatt QLD";

    [Fact]
    public async Task ScreenAsync_MapsAddresses_AndSetsUnknownRisk()
    {
        // Arrange
        var svc = new TestFloodScreeningService();
        var request = new FloodLookupRequest
        {
            Properties = new List<FloodLookupItem>
            {
                new() { Address = FakeStreetBrisbaneQld },
                new() { Address = MainRoadMountGravattQld }
            }
        };

        // Act
        FloodLookupResponse response = await svc.ScreenAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(2, response.Results.Count);

        FloodLookupResult result1 = response.Results[0];
        Assert.Equal(FakeStreetBrisbaneQld, result1.Address);
        Assert.Equal(FloodRisk.Unknown, result1.Risk);
        Assert.NotEmpty(result1.Reasons);

        FloodLookupResult result2 = response.Results[1];
        Assert.Equal(MainRoadMountGravattQld, result2.Address);
        Assert.Equal(FloodRisk.Unknown, result2.Risk);
        Assert.NotEmpty(result2.Reasons);
    }
}