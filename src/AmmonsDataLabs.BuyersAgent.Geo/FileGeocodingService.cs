using System.Text.Json;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace AmmonsDataLabs.BuyersAgent.Geo;

public sealed partial class FileGeocodingService : IGeocodingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly Lazy<Dictionary<string, AddressEntry>> _addressLookup;
    private readonly FileGeocodingOptions _options;

    public FileGeocodingService(IOptions<FileGeocodingOptions> options)
    {
        _options = options.Value;
        _addressLookup = new Lazy<Dictionary<string, AddressEntry>>(LoadAddresses);
    }

    public Task<GeocodingResult> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
            return Task.FromResult(new GeocodingResult
            {
                Query = address ?? string.Empty,
                Status = GeocodingStatus.Error,
                Provider = "FileGeocoding"
            });

        Dictionary<string, AddressEntry> lookup;
        try
        {
            lookup = _addressLookup.Value;
        }
        catch (Exception)
        {
            return Task.FromResult(new GeocodingResult
            {
                Query = address,
                Status = GeocodingStatus.Error,
                Provider = "FileGeocoding"
            });
        }

        var parsed = ParseAddress(address);
        if (parsed is not null)
        {
            var key = BuildLookupKey(parsed.Value);
            if (lookup.TryGetValue(key, out var entry))
                return Task.FromResult(new GeocodingResult
                {
                    Query = address,
                    NormalizedAddress = entry.NormalizedAddress,
                    Location = entry.Location,
                    Status = GeocodingStatus.Success,
                    Provider = "FileGeocoding",
                    LotPlan = entry.LotPlan
                });
        }

        return Task.FromResult(new GeocodingResult
        {
            Query = address,
            Status = GeocodingStatus.NotFound,
            Provider = "FileGeocoding"
        });
    }

    private Dictionary<string, AddressEntry> LoadAddresses()
    {
        var lookup = new Dictionary<string, AddressEntry>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(_options.FilePath) || !File.Exists(_options.FilePath)) return lookup;

        foreach (var line in File.ReadLines(_options.FilePath))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parcel = JsonSerializer.Deserialize<ParcelEntry>(line, JsonOptions);
            if (parcel is null || string.IsNullOrEmpty(parcel.LotPlan)) continue;

            GeoPoint? location = null;
            if (parcel.Latitude.HasValue && parcel.Longitude.HasValue)
                location = new GeoPoint(parcel.Latitude.Value, parcel.Longitude.Value);

            var entry = new AddressEntry(location, parcel.LotPlan, parcel.NormalizedAddress);

            var key = BuildLookupKey(
                parcel.UnitNumber,
                parcel.HouseNumber + (parcel.HouseNumberSuffix ?? ""),
                NormalizeStreetName(parcel.CorridorName, parcel.CorridorSuffixCode),
                parcel.Suburb);

            if (!string.IsNullOrEmpty(key)) lookup[key] = entry;
        }

        return lookup;
    }

    private static string BuildLookupKey(string? unit, string? house, string? street, string? suburb)
    {
        return
            $"{unit?.ToUpperInvariant() ?? ""}|{house?.ToUpperInvariant() ?? ""}|{street?.ToUpperInvariant() ?? ""}|{suburb?.ToUpperInvariant() ?? ""}";
    }

    private static string BuildLookupKey((string? unit, string? house, string? street, string? suburb) parsed)
    {
        return
            $"{parsed.unit?.ToUpperInvariant() ?? ""}|{parsed.house?.ToUpperInvariant() ?? ""}|{parsed.street?.ToUpperInvariant() ?? ""}|{parsed.suburb?.ToUpperInvariant() ?? ""}";
    }

    private static string NormalizeStreetName(string? corridorName, string? suffixCode)
    {
        if (string.IsNullOrEmpty(corridorName)) return "";

        var name = corridorName.ToUpperInvariant();
        if (!string.IsNullOrEmpty(suffixCode))
        {
            var normalized = NormalizeSuffix(suffixCode.ToUpperInvariant());
            name = $"{name} {normalized}";
        }

        return name;
    }

    private static string NormalizeSuffix(string suffix)
    {
        return suffix switch
        {
            "ST" or "STREET" => "ST",
            "RD" or "ROAD" => "RD",
            "DR" or "DRIVE" => "DR",
            "AVE" or "AV" or "AVENUE" => "AVE",
            "CT" or "COURT" => "CT",
            "CL" or "CLOSE" => "CL",
            "CR" or "CRES" or "CRESCENT" => "CR",
            "PL" or "PLACE" => "PL",
            "TCE" or "TERRACE" => "TCE",
            "LN" or "LANE" => "LN",
            "WAY" => "WAY",
            "BVD" or "BOULEVARD" => "BVD",
            "CCT" or "CIRCUIT" => "CCT",
            "GR" or "GROVE" => "GR",
            "PDE" or "PARADE" => "PDE",
            "HWY" or "HIGHWAY" => "HWY",
            "ESP" or "ESPLANADE" => "ESP",
            _ => suffix
        };
    }

    private static (string? unit, string? house, string? street, string? suburb)? ParseAddress(string address)
    {
        var match = AddressPattern().Match(address.Trim());
        if (!match.Success) return null;

        var unit = match.Groups["unit"].Success ? match.Groups["unit"].Value : null;
        var house = match.Groups["house"].Value;
        var street = match.Groups["street"].Value.Trim();
        var suburb = match.Groups["suburb"].Success ? match.Groups["suburb"].Value.Trim() : null;

        street = NormalizeInputStreetName(street);
        suburb = NormalizeSuburb(suburb);

        return (unit, house, street, suburb);
    }

    private static string? NormalizeSuburb(string? suburb)
    {
        if (string.IsNullOrEmpty(suburb)) return null;

        // Strip Australian state codes from end of suburb
        var upper = suburb.ToUpperInvariant();
        string[] stateCodes = ["QLD", "NSW", "VIC", "SA", "WA", "TAS", "NT", "ACT"];

        foreach (var state in stateCodes)
            if (upper.EndsWith($" {state}"))
                return suburb[..^(state.Length + 1)].Trim();

        return suburb;
    }

    private static string NormalizeInputStreetName(string street)
    {
        var words = street.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2) return street.ToUpperInvariant();

        var suffix = NormalizeSuffix(words[^1].ToUpperInvariant());
        var name = string.Join(" ", words[..^1]).ToUpperInvariant();

        return $"{name} {suffix}";
    }

    [GeneratedRegex(
        @"^(?:(?<unit>\d+)/)?(?<house>\d+[A-Za-z]?)\s+(?<street>[^,]+?)(?:,\s*(?<suburb>[^,\d]+?))?(?:\s+\d{4})?$",
        RegexOptions.IgnoreCase)]
    private static partial Regex AddressPattern();

    private sealed record AddressEntry(GeoPoint? Location, string? LotPlan, string? NormalizedAddress);

    [UsedImplicitly]
    private sealed class ParcelEntry
    {
        public string? LotPlan { get; set; }
        public string? UnitNumber { get; set; }
        public string? HouseNumber { get; set; }
        public string? HouseNumberSuffix { get; set; }
        public string? CorridorName { get; set; }
        public string? CorridorSuffixCode { get; set; }
        public string? Suburb { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? NormalizedAddress { get; set; }
    }
}