namespace AmmonsDataLabs.BuyersAgent.Geo;

public readonly record struct GeoPoint
{
    public GeoPoint(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(Latitude), latitude, "Latitude must be between -90 and 90.");

        if (longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(Longitude), longitude,
                "Longitude must be between -180 and 180.");

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }
    public double Longitude { get; }

    public override string ToString()
    {
        return $"({Latitude:F6}, {Longitude:F6})";
    }
}