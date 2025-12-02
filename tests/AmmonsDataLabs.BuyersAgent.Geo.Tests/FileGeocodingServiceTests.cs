using Microsoft.Extensions.Options;

namespace AmmonsDataLabs.BuyersAgent.Geo.Tests;

public class FileGeocodingServiceTests : IDisposable
{
    private readonly string _tempDir;

    public FileGeocodingServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"geocoding-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GeocodeAsync_KnownAddress_ReturnsSuccessWithLocation()
    {
        var filePath = CreateGeocodingFile(
        [
            new GeocodingEntry("1 Flood St, Brisbane QLD", -27.4710, 153.0250)
        ]);

        var options = Options.Create(new FileGeocodingOptions { FilePath = filePath });
        var sut = new FileGeocodingService(options);

        var result = await sut.GeocodeAsync("1 Flood St, Brisbane QLD");

        Assert.Equal(GeocodingStatus.Success, result.Status);
        Assert.NotNull(result.Location);
        Assert.Equal(-27.4710, result.Location.Value.Latitude, precision: 4);
        Assert.Equal(153.0250, result.Location.Value.Longitude, precision: 4);
        Assert.Equal("FileGeocoding", result.Provider);
    }

    [Fact]
    public async Task GeocodeAsync_UnknownAddress_ReturnsNotFound()
    {
        var filePath = CreateGeocodingFile(
        [
            new GeocodingEntry("1 Flood St, Brisbane QLD", -27.4710, 153.0250)
        ]);

        var options = Options.Create(new FileGeocodingOptions { FilePath = filePath });
        var sut = new FileGeocodingService(options);

        var result = await sut.GeocodeAsync("999 Unknown St, Brisbane QLD");

        Assert.Equal(GeocodingStatus.NotFound, result.Status);
        Assert.Null(result.Location);
    }

    [Fact]
    public async Task GeocodeAsync_CaseInsensitiveMatch_ReturnsSuccess()
    {
        var filePath = CreateGeocodingFile(
        [
            new GeocodingEntry("1 Flood St, Brisbane QLD", -27.4710, 153.0250)
        ]);

        var options = Options.Create(new FileGeocodingOptions { FilePath = filePath });
        var sut = new FileGeocodingService(options);

        var result = await sut.GeocodeAsync("1 FLOOD ST, BRISBANE QLD");

        Assert.Equal(GeocodingStatus.Success, result.Status);
        Assert.NotNull(result.Location);
    }

    [Fact]
    public async Task GeocodeAsync_EmptyAddress_ReturnsError()
    {
        var filePath = CreateGeocodingFile([]);

        var options = Options.Create(new FileGeocodingOptions { FilePath = filePath });
        var sut = new FileGeocodingService(options);

        var result = await sut.GeocodeAsync("");

        Assert.Equal(GeocodingStatus.Error, result.Status);
    }

    [Fact]
    public async Task GeocodeAsync_NullAddress_ReturnsError()
    {
        var filePath = CreateGeocodingFile([]);

        var options = Options.Create(new FileGeocodingOptions { FilePath = filePath });
        var sut = new FileGeocodingService(options);

        var result = await sut.GeocodeAsync(null!);

        Assert.Equal(GeocodingStatus.Error, result.Status);
    }

    [Fact]
    public async Task GeocodeAsync_MissingFile_ReturnsError()
    {
        var options = Options.Create(new FileGeocodingOptions { FilePath = "/nonexistent/file.json" });
        var sut = new FileGeocodingService(options);

        var result = await sut.GeocodeAsync("1 Flood St, Brisbane QLD");

        Assert.Equal(GeocodingStatus.Error, result.Status);
    }

    [Fact]
    public async Task GeocodeAsync_MultipleEntries_FindsCorrectOne()
    {
        var filePath = CreateGeocodingFile(
        [
            new GeocodingEntry("1 High Flood St, Brisbane QLD", -27.4000, 153.0000),
            new GeocodingEntry("1 Medium Flood St, Brisbane QLD", -27.4100, 153.0100),
            new GeocodingEntry("1 Low Flood St, Brisbane QLD", -27.4200, 153.0200)
        ]);

        var options = Options.Create(new FileGeocodingOptions { FilePath = filePath });
        var sut = new FileGeocodingService(options);

        var result = await sut.GeocodeAsync("1 Medium Flood St, Brisbane QLD");

        Assert.Equal(GeocodingStatus.Success, result.Status);
        Assert.NotNull(result.Location);
        Assert.Equal(-27.4100, result.Location.Value.Latitude, precision: 4);
        Assert.Equal(153.0100, result.Location.Value.Longitude, precision: 4);
    }

    private record GeocodingEntry(string Address, double Lat, double Lon);

    private string CreateGeocodingFile(GeocodingEntry[] entries)
    {
        var filePath = Path.Combine(_tempDir, "geocoding.json");
        var json = System.Text.Json.JsonSerializer.Serialize(
            entries.Select(e => new { address = e.Address, lat = e.Lat, lon = e.Lon }).ToArray());
        File.WriteAllText(filePath, json);
        return filePath;
    }
}
