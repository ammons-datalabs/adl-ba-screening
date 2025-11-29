namespace AmmonsDataLabs.BuyersAgent.Flood.Tests;

public class StubFloodScreeningServiceTests
{
    private const string FakeStreetBrisbaneQld = "123 Fake Street, Brisbane QLD";
    private const string MainRoadMountGravattQld = "456 Main Road, Mount Gravatt QLD";
    private const string UnknownAvenueSydneyNsw = "789 Unknown Avenue, Sydney NSW";

    private readonly StubFloodScreeningService _svc = new();

    [Fact]
    public async Task ScreenAsync_AddressContainsStreet_ReturnsLowRisk()
    {
        // Arrange
        var request = new FloodLookupRequest
        {
            Properties = [new FloodLookupItem { Address = FakeStreetBrisbaneQld }]
        };

        // Act
        var response = await _svc.ScreenAsync(request, CancellationToken.None);

        // Assert
        Assert.Single(response.Results);
        var result = response.Results[0];
        Assert.Equal(FakeStreetBrisbaneQld, result.Address);
        Assert.Equal(FloodRisk.Low, result.Risk);
        Assert.NotEmpty(result.Reasons);
    }

    [Fact]
    public async Task ScreenAsync_AddressContainsMainRoad_ReturnsHighRisk()
    {
        // Arrange
        var request = new FloodLookupRequest
        {
            Properties = [new FloodLookupItem { Address = MainRoadMountGravattQld }]
        };

        // Act
        var response = await _svc.ScreenAsync(request, CancellationToken.None);

        // Assert
        Assert.Single(response.Results);
        var result = response.Results[0];
        Assert.Equal(MainRoadMountGravattQld, result.Address);
        Assert.Equal(FloodRisk.High, result.Risk);
        Assert.NotEmpty(result.Reasons);
    }

    [Fact]
    public async Task ScreenAsync_AddressMatchesNoPattern_ReturnsUnknownRisk()
    {
        // Arrange
        var request = new FloodLookupRequest
        {
            Properties = [new FloodLookupItem { Address = UnknownAvenueSydneyNsw }]
        };

        // Act
        var response = await _svc.ScreenAsync(request, CancellationToken.None);

        // Assert
        Assert.Single(response.Results);
        var result = response.Results[0];
        Assert.Equal(UnknownAvenueSydneyNsw, result.Address);
        Assert.Equal(FloodRisk.Unknown, result.Risk);
        Assert.NotEmpty(result.Reasons);
    }

    [Fact]
    public async Task ScreenAsync_MultipleAddresses_ReturnsMappedResults()
    {
        // Arrange
        var request = new FloodLookupRequest
        {
            Properties =
            [
                new FloodLookupItem { Address = FakeStreetBrisbaneQld },
                new FloodLookupItem { Address = MainRoadMountGravattQld },
                new FloodLookupItem { Address = UnknownAvenueSydneyNsw }
            ]
        };

        // Act
        var response = await _svc.ScreenAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(3, response.Results.Count);

        Assert.Equal(FloodRisk.Low, response.Results[0].Risk);
        Assert.Equal(FloodRisk.High, response.Results[1].Risk);
        Assert.Equal(FloodRisk.Unknown, response.Results[2].Risk);
    }

    [Fact]
    public async Task ScreenAsync_ResultsAlwaysHaveNonNullReasons()
    {
        // Arrange
        var request = new FloodLookupRequest
        {
            Properties =
            [
                new FloodLookupItem { Address = FakeStreetBrisbaneQld },
                new FloodLookupItem { Address = MainRoadMountGravattQld }
            ]
        };

        // Act
        var response = await _svc.ScreenAsync(request, CancellationToken.None);

        // Assert
        Assert.All(response.Results, r =>
        {
            Assert.NotNull(r.Reasons);
            Assert.NotEmpty(r.Reasons);
        });
    }
}