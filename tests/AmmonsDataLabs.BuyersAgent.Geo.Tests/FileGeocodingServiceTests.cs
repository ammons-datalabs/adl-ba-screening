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
    public async Task GeocodeAsync_EmptyAddress_ReturnsError()
    {
        var options = Options.Create(new FileGeocodingOptions { FilePath = CreateEmptyNdjsonFile() });
        var sut = new FileGeocodingService(options);

        var result = await sut.GeocodeAsync("");

        Assert.Equal(GeocodingStatus.Error, result.Status);
    }

    [Fact]
    public async Task GeocodeAsync_NullAddress_ReturnsError()
    {
        var options = Options.Create(new FileGeocodingOptions { FilePath = CreateEmptyNdjsonFile() });
        var sut = new FileGeocodingService(options);

        var result = await sut.GeocodeAsync(null!);

        Assert.Equal(GeocodingStatus.Error, result.Status);
    }

    [Fact]
    public async Task GeocodeAsync_MissingFile_ReturnsNotFound()
    {
        var options = Options.Create(new FileGeocodingOptions { FilePath = "/nonexistent/file.ndjson" });
        var sut = new FileGeocodingService(options);

        var result = await sut.GeocodeAsync("117 Fernberg Road, Paddington");

        Assert.Equal(GeocodingStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GeocodeAsync_UnknownAddress_ReturnsNotFound()
    {
        var filePath = CreateNdjsonParcelFile(new[]
        {
            CreateFernberg117Entry()
        });

        var options = Options.Create(new FileGeocodingOptions { FilePath = filePath });
        var sut = new FileGeocodingService(options);

        var result = await sut.GeocodeAsync("999 Unknown Street, Brisbane");

        Assert.Equal(GeocodingStatus.NotFound, result.Status);
        Assert.Null(result.Location);
    }

    [Fact]
    public async Task GeocodeAsync_HorizonDrive_ReturnsLotPlanAndLocation()
    {
        // Real data from BCC property-boundaries-parcel.geojson
        // 3/241 Horizon Drive, Westlake - Unit 3 in GTP102995 complex
        var filePath = CreateNdjsonParcelFile(new[]
        {
            new ParcelEntry
            {
                LotPlan = "3GTP102995",
                UnitNumber = "3",
                HouseNumber = "241",
                CorridorName = "HORIZON",
                CorridorSuffixCode = "DR",
                Suburb = "WESTLAKE",
                Latitude = -27.549067,
                Longitude = 152.911153,
                NormalizedAddress = "3/241 Horizon Drive, Westlake"
            }
        });

        var options = Options.Create(new FileGeocodingOptions { FilePath = filePath });
        var sut = new FileGeocodingService(options);

        var result = await sut.GeocodeAsync("3/241 Horizon Drive, Westlake");

        Assert.Equal(GeocodingStatus.Success, result.Status);
        Assert.Equal("3GTP102995", result.LotPlan);
        Assert.NotNull(result.Location);
        Assert.Equal(-27.549067, result.Location!.Value.Latitude, precision: 6);
        Assert.Equal(152.911153, result.Location!.Value.Longitude, precision: 6);
        Assert.Equal("FileGeocoding", result.Provider);
    }

    [Fact]
    public async Task GeocodeAsync_Fernberg117_NormalizesStreetSuffix()
    {
        // Real data from BCC - 117 Fernberg Road, Paddington (no flood risk)
        var filePath = CreateNdjsonParcelFile(new[]
        {
            CreateFernberg117Entry()
        });

        var options = Options.Create(new FileGeocodingOptions { FilePath = filePath });
        var sut = new FileGeocodingService(options);

        // Test with "Road" instead of "RD" - suffix normalization
        var result = await sut.GeocodeAsync("117 Fernberg Road, Paddington");

        Assert.Equal(GeocodingStatus.Success, result.Status);
        Assert.Equal("1RP84382", result.LotPlan);
    }

    [Fact]
    public async Task GeocodeAsync_Fernberg118_CaseInsensitive()
    {
        // Real data from BCC - 118 Fernberg Road, Paddington (creek flash flooding risk)
        var filePath = CreateNdjsonParcelFile(new[]
        {
            new ParcelEntry
            {
                LotPlan = "20SP191298",
                HouseNumber = "118",
                CorridorName = "FERNBERG",
                CorridorSuffixCode = "RD",
                Suburb = "PADDINGTON",
                Latitude = -27.459721,
                Longitude = 152.993156,
                NormalizedAddress = "118 Fernberg Road, Paddington"
            }
        });

        var options = Options.Create(new FileGeocodingOptions { FilePath = filePath });
        var sut = new FileGeocodingService(options);

        // Test case-insensitive matching
        var result = await sut.GeocodeAsync("118 fernberg road, paddington");

        Assert.Equal(GeocodingStatus.Success, result.Status);
        Assert.Equal("20SP191298", result.LotPlan);
    }

    [Fact]
    public async Task GeocodeAsync_BellambiPlace_MarginalRiverRisk()
    {
        // Real data from BCC - 5 Bellambi Place, Westlake QLD 4074 (marginal river risk, floor 13.5m AHD, RFL 13.6m)
        var filePath = CreateNdjsonParcelFile(new[]
        {
            new ParcelEntry
            {
                LotPlan = "678RP866225",
                HouseNumber = "5",
                CorridorName = "BELLAMBI",
                CorridorSuffixCode = "PL",
                Suburb = "WESTLAKE",
                Latitude = -27.549234,
                Longitude = 152.912456,
                NormalizedAddress = "5 Bellambi Place, Westlake"
            }
        });

        var options = Options.Create(new FileGeocodingOptions { FilePath = filePath });
        var sut = new FileGeocodingService(options);

        var result = await sut.GeocodeAsync("5 Bellambi Place, Westlake");

        Assert.Equal(GeocodingStatus.Success, result.Status);
        Assert.Equal("678RP866225", result.LotPlan);
        Assert.NotNull(result.Location);
    }

    [Fact]
    public async Task GeocodeAsync_MultipleEntries_FindsCorrectOne()
    {
        var filePath = CreateNdjsonParcelFile(new[]
        {
            CreateFernberg117Entry(),
            new ParcelEntry
            {
                LotPlan = "20SP191298",
                HouseNumber = "118",
                CorridorName = "FERNBERG",
                CorridorSuffixCode = "RD",
                Suburb = "PADDINGTON",
                Latitude = -27.459721,
                Longitude = 152.993156,
                NormalizedAddress = "118 Fernberg Road, Paddington"
            }
        });

        var options = Options.Create(new FileGeocodingOptions { FilePath = filePath });
        var sut = new FileGeocodingService(options);

        var result117 = await sut.GeocodeAsync("117 Fernberg Rd, Paddington");
        var result118 = await sut.GeocodeAsync("118 Fernberg Rd, Paddington");

        Assert.Equal("1RP84382", result117.LotPlan);
        Assert.Equal("20SP191298", result118.LotPlan);
    }

    private static ParcelEntry CreateFernberg117Entry() => new()
    {
        LotPlan = "1RP84382",
        HouseNumber = "117",
        CorridorName = "FERNBERG",
        CorridorSuffixCode = "RD",
        Suburb = "PADDINGTON",
        Latitude = -27.459876,
        Longitude = 152.992834,
        NormalizedAddress = "117 Fernberg Road, Paddington"
    };

    private record ParcelEntry
    {
        public string? LotPlan { get; init; }
        public string? UnitNumber { get; init; }
        public string? HouseNumber { get; init; }
        public string? HouseNumberSuffix { get; init; }
        public string? CorridorName { get; init; }
        public string? CorridorSuffixCode { get; init; }
        public string? Suburb { get; init; }
        public double? Latitude { get; init; }
        public double? Longitude { get; init; }
        public string? NormalizedAddress { get; init; }
    }

    private string CreateEmptyNdjsonFile()
    {
        var filePath = Path.Combine(_tempDir, "empty.ndjson");
        File.WriteAllText(filePath, "");
        return filePath;
    }

    private string CreateNdjsonParcelFile(ParcelEntry[] entries)
    {
        var filePath = Path.Combine(_tempDir, "addresses.ndjson");
        using var writer = File.CreateText(filePath);
        foreach (var entry in entries)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new
            {
                lot_plan = entry.LotPlan,
                unit_number = entry.UnitNumber,
                house_number = entry.HouseNumber,
                house_number_suffix = entry.HouseNumberSuffix,
                corridor_name = entry.CorridorName,
                corridor_suffix_code = entry.CorridorSuffixCode,
                suburb = entry.Suburb,
                latitude = entry.Latitude,
                longitude = entry.Longitude,
                normalized_address = entry.NormalizedAddress
            });
            writer.WriteLine(json);
        }
        return filePath;
    }
}
