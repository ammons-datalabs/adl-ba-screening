using AmmonsDataLabs.BuyersAgent.Flood;
using AmmonsDataLabs.BuyersAgent.Screening.Api.Services;

namespace AmmonsDataLabs.BuyersAgent.Screening.Api.Tests;

public class SimpleFloodDataProviderTests
{
    private readonly SimpleFloodDataProvider _provider = new();

    [Theory]
    [InlineData("456 Main Road, Mount Gravatt QLD")]
    [InlineData("123 Main Rd, Brisbane QLD")]
    [InlineData("1 Pacific Motorway, Gold Coast QLD")]
    [InlineData("500 Bruce Highway, Caboolture QLD")]
    public async Task LookupAsync_MajorRoadAddress_ReturnsHighRisk(string address)
    {
        var result = await _provider.LookupAsync(address, CancellationToken.None);

        Assert.Equal(address, result.Address);
        Assert.Equal(FloodRisk.High, result.Risk);
        Assert.NotEmpty(result.Reasons);
    }

    [Theory]
    [InlineData("10 Oxley Creek Road, Oxley QLD")]
    [InlineData("25 River Terrace, Kangaroo Point QLD")]
    [InlineData("5 Breakfast Ck Road, Newstead QLD")]
    public async Task LookupAsync_WaterwayAddress_ReturnsMediumRisk(string address)
    {
        var result = await _provider.LookupAsync(address, CancellationToken.None);

        Assert.Equal(address, result.Address);
        Assert.Equal(FloodRisk.Medium, result.Risk);
        Assert.NotEmpty(result.Reasons);
    }

    [Theory]
    [InlineData("123 Fake Street, Brisbane QLD")]
    [InlineData("789 Unknown Avenue, Sydney NSW")]
    public async Task LookupAsync_NoFloodIndicators_ReturnsLowRisk(string address)
    {
        var result = await _provider.LookupAsync(address, CancellationToken.None);

        Assert.Equal(address, result.Address);
        Assert.Equal(FloodRisk.Low, result.Risk);
        Assert.NotEmpty(result.Reasons);
    }

    [Fact]
    public async Task LookupAsync_HighRiskTakesPrecedenceOverMedium()
    {
        // Address contains both "Main Road" and "Creek"
        var result = await _provider.LookupAsync(
            "1 Main Road, Creek View QLD", CancellationToken.None);

        Assert.Equal(FloodRisk.High, result.Risk);
    }
}