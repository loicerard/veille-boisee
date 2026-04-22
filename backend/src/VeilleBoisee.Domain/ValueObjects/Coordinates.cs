using VeilleBoisee.Domain.Common;

namespace VeilleBoisee.Domain.ValueObjects;

public readonly record struct Coordinates
{
    public const double FranceLatitudeMin = 41.2;
    public const double FranceLatitudeMax = 51.2;
    public const double FranceLongitudeMin = -5.5;
    public const double FranceLongitudeMax = 9.6;

    public double Latitude { get; }
    public double Longitude { get; }

    private Coordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static Result<Coordinates, CoordinatesError> Create(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || !double.IsFinite(longitude))
        {
            return CoordinatesError.NotFinite;
        }

        if (latitude < FranceLatitudeMin || latitude > FranceLatitudeMax
            || longitude < FranceLongitudeMin || longitude > FranceLongitudeMax)
        {
            return CoordinatesError.OutOfFranceBounds;
        }

        return new Coordinates(latitude, longitude);
    }
}
