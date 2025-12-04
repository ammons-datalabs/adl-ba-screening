using System.Text.Json;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep;

/// <summary>
/// Reads BCC property-boundaries-parcel GeoJSON and extracts address-to-lotplan mappings.
/// </summary>
public static class ParcelAddressDataPrep
{
    /// <summary>
    /// Processes a BCC parcel boundaries GeoJSON file and produces address lookup records.
    /// </summary>
    public static IEnumerable<AddressLookupRecord> Run(string geoJsonPath)
    {
        using var fs = File.OpenText(geoJsonPath);
        var serializer = GeoJsonSerializer.Create();
        var collection = (FeatureCollection)serializer.Deserialize(fs, typeof(FeatureCollection))!;

        foreach (var feature in collection)
        {
            var attrs = feature.Attributes;

            var lotplan = GetString(attrs, "lotplan");
            var plan = GetString(attrs, "plan");

            if (string.IsNullOrEmpty(lotplan) || string.IsNullOrEmpty(plan))
                continue;

            var unitNumber = GetString(attrs, "unit_number");
            var houseNumber = GetString(attrs, "house_number");
            var houseNumberSuffix = GetString(attrs, "house_number_suffix");
            var corridorName = GetString(attrs, "corridor_name");
            var corridorSuffixCode = GetString(attrs, "corridor_suffix_code");
            var suburb = GetString(attrs, "suburb");
            var postcode = GetString(attrs, "postcode");

            // Get centroid from geometry
            double? lat = null;
            double? lon = null;
            if (feature.Geometry is Geometry geom)
            {
                var centroid = geom.Centroid;
                lat = centroid.Y;
                lon = centroid.X;
            }

            // Build normalized address
            var normalizedAddress = BuildNormalizedAddress(
                unitNumber, houseNumber, houseNumberSuffix,
                corridorName, corridorSuffixCode, suburb, postcode);

            yield return new AddressLookupRecord
            {
                LotPlan = lotplan,
                Plan = plan,
                UnitNumber = unitNumber,
                HouseNumber = houseNumber,
                HouseNumberSuffix = houseNumberSuffix,
                CorridorName = corridorName,
                CorridorSuffixCode = corridorSuffixCode,
                Suburb = suburb,
                Postcode = postcode,
                Latitude = lat,
                Longitude = lon,
                NormalizedAddress = normalizedAddress
            };
        }
    }

    private static string? GetString(IAttributesTable attrs, string name)
    {
        if (!attrs.Exists(name)) return null;
        var val = attrs[name];
        if (val is null) return null;
        var s = val.ToString();
        return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }

    private static string? BuildNormalizedAddress(
        string? unitNumber,
        string? houseNumber,
        string? houseNumberSuffix,
        string? corridorName,
        string? corridorSuffixCode,
        string? suburb,
        string? postcode)
    {
        if (string.IsNullOrEmpty(corridorName))
            return null;

        var parts = new List<string>();

        // Unit/house number
        var streetNum = houseNumber ?? "";
        if (!string.IsNullOrEmpty(houseNumberSuffix))
            streetNum += houseNumberSuffix;

        if (!string.IsNullOrEmpty(unitNumber) && !string.IsNullOrEmpty(streetNum))
        {
            parts.Add($"{unitNumber}/{streetNum}");
        }
        else if (!string.IsNullOrEmpty(streetNum))
        {
            parts.Add(streetNum);
        }

        // Street name
        var street = corridorName;
        if (!string.IsNullOrEmpty(corridorSuffixCode))
        {
            street += " " + ExpandStreetSuffix(corridorSuffixCode);
        }
        parts.Add(street);

        // Suburb
        if (!string.IsNullOrEmpty(suburb))
        {
            parts.Add(suburb);
        }

        // Postcode
        if (!string.IsNullOrEmpty(postcode))
        {
            parts.Add(postcode);
        }

        return string.Join(", ", parts);
    }

    private static string ExpandStreetSuffix(string code)
    {
        return code.ToUpperInvariant() switch
        {
            "ST" => "Street",
            "RD" => "Road",
            "DR" => "Drive",
            "AVE" => "Avenue",
            "AV" => "Avenue",
            "CT" => "Court",
            "CL" => "Close",
            "CR" => "Crescent",
            "CRES" => "Crescent",
            "PL" => "Place",
            "TCE" => "Terrace",
            "LN" => "Lane",
            "WAY" => "Way",
            "BVD" => "Boulevard",
            "CCT" => "Circuit",
            "GR" => "Grove",
            "PDE" => "Parade",
            "HWY" => "Highway",
            "ESP" => "Esplanade",
            _ => code
        };
    }
}
