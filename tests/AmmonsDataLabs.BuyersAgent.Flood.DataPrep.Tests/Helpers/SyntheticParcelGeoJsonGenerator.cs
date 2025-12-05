using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace AmmonsDataLabs.BuyersAgent.Flood.DataPrep.Tests.Helpers;

public static class SyntheticParcelGeoJsonGenerator
{
    public static void GenerateParcelGeoJson(string outputPath, params ParcelData[] parcels)
    {
        var factory = new GeometryFactory();
        var featureCollection = new FeatureCollection();

        foreach (var parcel in parcels)
        {
            var feature = CreateParcelFeature(factory, parcel);
            featureCollection.Add(feature);
        }

        var serializer = GeoJsonSerializer.Create();
        using var writer = File.CreateText(outputPath);
        serializer.Serialize(writer, featureCollection);
    }

    private static Feature CreateParcelFeature(GeometryFactory factory, ParcelData parcel)
    {
        // Create a small polygon around the centroid
        var offset = 0.0001;
        var ring = new[]
        {
            new Coordinate(parcel.Longitude - offset, parcel.Latitude - offset),
            new Coordinate(parcel.Longitude + offset, parcel.Latitude - offset),
            new Coordinate(parcel.Longitude + offset, parcel.Latitude + offset),
            new Coordinate(parcel.Longitude - offset, parcel.Latitude + offset),
            new Coordinate(parcel.Longitude - offset, parcel.Latitude - offset) // Close the ring
        };

        var polygon = factory.CreatePolygon(ring);
        polygon.SRID = 4326;

        var attributes = new AttributesTable();

        // Required fields
        if (parcel.LotPlan != null) attributes.Add("lotplan", parcel.LotPlan);
        if (parcel.Plan != null) attributes.Add("plan", parcel.Plan);

        // Optional address fields
        if (parcel.UnitNumber != null) attributes.Add("unit_number", parcel.UnitNumber);
        if (parcel.HouseNumber != null) attributes.Add("house_number", parcel.HouseNumber);
        if (parcel.HouseNumberSuffix != null) attributes.Add("house_number_suffix", parcel.HouseNumberSuffix);
        if (parcel.CorridorName != null) attributes.Add("corridor_name", parcel.CorridorName);
        if (parcel.CorridorSuffixCode != null) attributes.Add("corridor_suffix_code", parcel.CorridorSuffixCode);
        if (parcel.Suburb != null) attributes.Add("suburb", parcel.Suburb);
        if (parcel.Postcode != null) attributes.Add("postcode", parcel.Postcode);

        return new Feature(polygon, attributes);
    }

    public static Feature CreateFeatureWithoutGeometry(ParcelData parcel)
    {
        var attributes = new AttributesTable();
        if (parcel.LotPlan != null) attributes.Add("lotplan", parcel.LotPlan);
        if (parcel.Plan != null) attributes.Add("plan", parcel.Plan);
        if (parcel.CorridorName != null) attributes.Add("corridor_name", parcel.CorridorName);
        if (parcel.CorridorSuffixCode != null) attributes.Add("corridor_suffix_code", parcel.CorridorSuffixCode);

        return new Feature(null, attributes);
    }
}

public record ParcelData
{
    public string? LotPlan { get; init; }
    public string? Plan { get; init; }
    public string? UnitNumber { get; init; }
    public string? HouseNumber { get; init; }
    public string? HouseNumberSuffix { get; init; }
    public string? CorridorName { get; init; }
    public string? CorridorSuffixCode { get; init; }
    public string? Suburb { get; init; }
    public string? Postcode { get; init; }
    public double Latitude { get; init; } = -27.47;
    public double Longitude { get; init; } = 153.02;
}