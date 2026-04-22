using FluentAssertions;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Domain.Tests.ValueObjects;

public class CoordinatesTests
{
    [Theory]
    [InlineData(48.8566, 2.3522)]   // Paris
    [InlineData(43.2965, 5.3698)]   // Marseille
    [InlineData(50.6292, 3.0573)]   // Lille
    [InlineData(41.9279, 8.7372)]   // Ajaccio (Corse)
    public void Create_with_valid_coordinates_in_metropolitan_france_succeeds(double latitude, double longitude)
    {
        var result = Coordinates.Create(latitude, longitude);

        result.IsSuccess.Should().BeTrue();
        result.Value.Latitude.Should().Be(latitude);
        result.Value.Longitude.Should().Be(longitude);
    }

    [Theory]
    [InlineData(Coordinates.FranceLatitudeMin, Coordinates.FranceLongitudeMin)]
    [InlineData(Coordinates.FranceLatitudeMax, Coordinates.FranceLongitudeMax)]
    public void Create_with_coordinates_on_france_bounds_succeeds(double latitude, double longitude)
    {
        var result = Coordinates.Create(latitude, longitude);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(52.0, 2.0)]          // lat > max (Belgium)
    [InlineData(40.0, 2.0)]          // lat < min (Spain)
    [InlineData(48.8566, -10.0)]     // lon < min (Atlantic)
    [InlineData(48.8566, 15.0)]      // lon > max (Germany)
    [InlineData(0.0, 0.0)]            // Middle of Atlantic
    public void Create_with_coordinates_outside_france_fails(double latitude, double longitude)
    {
        var result = Coordinates.Create(latitude, longitude);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CoordinatesError.OutOfFranceBounds);
    }

    [Theory]
    [InlineData(double.NaN, 2.0)]
    [InlineData(48.0, double.NaN)]
    [InlineData(double.PositiveInfinity, 2.0)]
    [InlineData(48.0, double.NegativeInfinity)]
    public void Create_with_non_finite_values_fails(double latitude, double longitude)
    {
        var result = Coordinates.Create(latitude, longitude);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CoordinatesError.NotFinite);
    }
}
