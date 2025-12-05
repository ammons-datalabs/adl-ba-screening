namespace AmmonsDataLabs.BuyersAgent.Geo.Tests;

public class GeocodingResultTests
{
    [Fact]
    public void SuccessResult_HasLocationAndNormalizedAddress()
    {
        var point = new GeoPoint(-27.4705, 153.0260);

        var result = new GeocodingResult
        {
            Query = "25 Random St, Brisbane QLD",
            NormalizedAddress = "25 Random Street, Brisbane QLD 4000",
            Location = point,
            Status = GeocodingStatus.Success,
            Provider = "TestProvider"
        };

        Assert.Equal(GeocodingStatus.Success, result.Status);
        Assert.Equal(point, result.Location);
        Assert.Equal("25 Random Street, Brisbane QLD 4000", result.NormalizedAddress);
        Assert.Equal("TestProvider", result.Provider);
        Assert.Equal("25 Random St, Brisbane QLD", result.Query);
    }

    [Fact]
    public void NotFoundResult_HasNoLocation()
    {
        var result = new GeocodingResult
        {
            Query = "Made up address",
            Status = GeocodingStatus.NotFound
        };

        Assert.Equal(GeocodingStatus.NotFound, result.Status);
        Assert.Null(result.Location);
        Assert.Null(result.NormalizedAddress);
        Assert.Null(result.Provider);
    }
}