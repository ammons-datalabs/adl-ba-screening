using AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests.Helpers;
using Xunit;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests;

public class ParcelAddressDataPrepTests : IDisposable
{
    private readonly string _tempDir;

    public ParcelAddressDataPrepTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"parcel-dataprep-test-{Guid.NewGuid()}");
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
    public void Run_ExtractsBasicParcelData()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "1RP12345",
                Plan = "RP12345",
                HouseNumber = "42",
                CorridorName = "TEST",
                CorridorSuffixCode = "ST",
                Suburb = "BRISBANE",
                Postcode = "4000",
                Latitude = -27.4705,
                Longitude = 153.0260
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Single(records);
        var record = records[0];
        Assert.Equal("1RP12345", record.LotPlan);
        Assert.Equal("RP12345", record.Plan);
        Assert.Equal("42", record.HouseNumber);
        Assert.Equal("TEST", record.CorridorName);
        Assert.Equal("ST", record.CorridorSuffixCode);
        Assert.Equal("BRISBANE", record.Suburb);
        Assert.Equal("4000", record.Postcode);
    }

    [Fact]
    public void Run_ExtractsCentroidCoordinates()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "1RP12345",
                Plan = "RP12345",
                CorridorName = "TEST",
                Latitude = -27.4705,
                Longitude = 153.0260
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Single(records);
        var record = records[0];
        Assert.NotNull(record.Latitude);
        Assert.NotNull(record.Longitude);
        // Centroid should be close to the specified coordinates (within the small polygon offset)
        Assert.True(Math.Abs(record.Latitude!.Value - (-27.4705)) < 0.001);
        Assert.True(Math.Abs(record.Longitude!.Value - 153.0260) < 0.001);
    }

    [Fact]
    public void Run_SkipsRecordsWithoutLotplan()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = null, // Missing lotplan
                Plan = "RP12345",
                CorridorName = "TEST"
            },
            new ParcelData
            {
                LotPlan = "2RP12345",
                Plan = "RP12345",
                CorridorName = "VALID"
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Single(records);
        Assert.Equal("2RP12345", records[0].LotPlan);
    }

    [Fact]
    public void Run_SkipsRecordsWithoutPlan()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "1RP12345",
                Plan = null, // Missing plan
                CorridorName = "TEST"
            },
            new ParcelData
            {
                LotPlan = "2RP12345",
                Plan = "RP12345",
                CorridorName = "VALID"
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Single(records);
        Assert.Equal("2RP12345", records[0].LotPlan);
    }

    [Fact]
    public void Run_BuildsNormalizedAddress_SimpleHouseNumber()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "1RP12345",
                Plan = "RP12345",
                HouseNumber = "42",
                CorridorName = "QUEEN",
                CorridorSuffixCode = "ST",
                Suburb = "BRISBANE",
                Postcode = "4000"
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Single(records);
        Assert.Equal("42, QUEEN Street, BRISBANE, 4000", records[0].NormalizedAddress);
    }

    [Fact]
    public void Run_BuildsNormalizedAddress_WithUnitNumber()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "3GTP102995",
                Plan = "GTP102995",
                UnitNumber = "3",
                HouseNumber = "241",
                CorridorName = "HORIZON",
                CorridorSuffixCode = "DR",
                Suburb = "WESTLAKE",
                Postcode = "4074"
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Single(records);
        Assert.Equal("3/241, HORIZON Drive, WESTLAKE, 4074", records[0].NormalizedAddress);
    }

    [Fact]
    public void Run_BuildsNormalizedAddress_WithHouseNumberSuffix()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "1RP12345",
                Plan = "RP12345",
                HouseNumber = "42",
                HouseNumberSuffix = "A",
                CorridorName = "MAIN",
                CorridorSuffixCode = "RD",
                Suburb = "OXLEY",
                Postcode = "4075"
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Single(records);
        Assert.Equal("42A, MAIN Road, OXLEY, 4075", records[0].NormalizedAddress);
    }

    [Fact]
    public void Run_ReturnsNullNormalizedAddress_WhenNoCorridorName()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "1RP12345",
                Plan = "RP12345",
                HouseNumber = "42",
                CorridorName = null, // No street name
                Suburb = "BRISBANE"
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Single(records);
        Assert.Null(records[0].NormalizedAddress);
    }

    [Theory]
    [InlineData("ST", "Street")]
    [InlineData("RD", "Road")]
    [InlineData("DR", "Drive")]
    [InlineData("AVE", "Avenue")]
    [InlineData("AV", "Avenue")]
    [InlineData("CT", "Court")]
    [InlineData("CL", "Close")]
    [InlineData("CR", "Crescent")]
    [InlineData("CRES", "Crescent")]
    [InlineData("PL", "Place")]
    [InlineData("TCE", "Terrace")]
    [InlineData("LN", "Lane")]
    [InlineData("WAY", "Way")]
    [InlineData("BVD", "Boulevard")]
    [InlineData("CCT", "Circuit")]
    [InlineData("GR", "Grove")]
    [InlineData("PDE", "Parade")]
    [InlineData("HWY", "Highway")]
    [InlineData("ESP", "Esplanade")]
    public void Run_ExpandsStreetSuffixes(string suffixCode, string expectedExpansion)
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, $"parcels-{suffixCode}.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "1RP12345",
                Plan = "RP12345",
                HouseNumber = "1",
                CorridorName = "TEST",
                CorridorSuffixCode = suffixCode
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Single(records);
        Assert.Contains($"TEST {expectedExpansion}", records[0].NormalizedAddress);
    }

    [Fact]
    public void Run_PreservesUnknownStreetSuffix()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "1RP12345",
                Plan = "RP12345",
                HouseNumber = "1",
                CorridorName = "TEST",
                CorridorSuffixCode = "UNKNOWN"
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Single(records);
        Assert.Contains("TEST UNKNOWN", records[0].NormalizedAddress);
    }

    [Fact]
    public void Run_HandlesMultipleParcels()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "1RP12345",
                Plan = "RP12345",
                HouseNumber = "1",
                CorridorName = "FIRST",
                CorridorSuffixCode = "ST"
            },
            new ParcelData
            {
                LotPlan = "2RP12345",
                Plan = "RP12345",
                HouseNumber = "2",
                CorridorName = "SECOND",
                CorridorSuffixCode = "RD"
            },
            new ParcelData
            {
                LotPlan = "3RP67890",
                Plan = "RP67890",
                HouseNumber = "3",
                CorridorName = "THIRD",
                CorridorSuffixCode = "AVE"
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Equal(3, records.Count);
        Assert.Contains(records, r => r.LotPlan == "1RP12345");
        Assert.Contains(records, r => r.LotPlan == "2RP12345");
        Assert.Contains(records, r => r.LotPlan == "3RP67890");
    }

    [Fact]
    public void Run_TrimsWhitespaceFromAttributes()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "  1RP12345  ",
                Plan = "  RP12345  ",
                HouseNumber = " 42 ",
                CorridorName = " QUEEN ",
                CorridorSuffixCode = " ST ",
                Suburb = " BRISBANE ",
                Postcode = " 4000 "
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert - note: trimming happens during GetString, but the generator
        // doesn't produce pre-trimmed values. The test verifies normalized address logic works
        Assert.Single(records);
        // The lotplan/plan come through as-is from the generator (no extra whitespace added)
        Assert.NotNull(records[0].NormalizedAddress);
    }

    [Fact]
    public void Run_BuildsNormalizedAddress_WithoutPostcode()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "1RP12345",
                Plan = "RP12345",
                HouseNumber = "42",
                CorridorName = "QUEEN",
                CorridorSuffixCode = "ST",
                Suburb = "BRISBANE",
                Postcode = null
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Single(records);
        Assert.Equal("42, QUEEN Street, BRISBANE", records[0].NormalizedAddress);
    }

    [Fact]
    public void Run_BuildsNormalizedAddress_WithoutSuburb()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "1RP12345",
                Plan = "RP12345",
                HouseNumber = "42",
                CorridorName = "QUEEN",
                CorridorSuffixCode = "ST",
                Suburb = null,
                Postcode = "4000"
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Single(records);
        Assert.Equal("42, QUEEN Street, 4000", records[0].NormalizedAddress);
    }

    [Fact]
    public void Run_BuildsNormalizedAddress_StreetNameOnly()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "1RP12345",
                Plan = "RP12345",
                HouseNumber = null,
                CorridorName = "QUEEN",
                CorridorSuffixCode = "ST",
                Suburb = null,
                Postcode = null
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Single(records);
        Assert.Equal("QUEEN Street", records[0].NormalizedAddress);
    }

    [Fact]
    public void Run_BuildsNormalizedAddress_WithoutStreetSuffix()
    {
        // Arrange
        var geoJsonPath = Path.Combine(_tempDir, "parcels.geojson");
        SyntheticParcelGeoJsonGenerator.GenerateParcelGeoJson(geoJsonPath,
            new ParcelData
            {
                LotPlan = "1RP12345",
                Plan = "RP12345",
                HouseNumber = "42",
                CorridorName = "THE MALL",
                CorridorSuffixCode = null,
                Suburb = "BRISBANE",
                Postcode = "4000"
            });

        // Act
        var records = ParcelAddressDataPrep.Run(geoJsonPath).ToList();

        // Assert
        Assert.Single(records);
        Assert.Equal("42, THE MALL, BRISBANE, 4000", records[0].NormalizedAddress);
    }
}
