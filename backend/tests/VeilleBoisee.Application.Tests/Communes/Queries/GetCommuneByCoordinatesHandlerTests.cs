using FluentAssertions;
using NSubstitute;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Application.Communes.Queries;
using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Application.Tests.Communes.Queries;

public class GetCommuneByCoordinatesHandlerTests
{
    private readonly IGeocodingService _geocoding = Substitute.For<IGeocodingService>();

    [Fact]
    public async Task Returns_commune_when_geocoding_finds_one()
    {
        // Arrange
        var paris = Commune.Create(CodeInsee.Create("75056").Value, "Paris");
        _geocoding.FindCommuneAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>())
            .Returns(GeocodingOutcome.Found(paris));
        var handler = new GetCommuneByCoordinatesHandler(_geocoding);

        // Act
        var result = await handler.Handle(new GetCommuneByCoordinatesQuery(48.8566, 2.3522), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(paris);
    }

    [Fact]
    public async Task Returns_invalid_coordinates_when_outside_france_bounds()
    {
        // Arrange
        var handler = new GetCommuneByCoordinatesHandler(_geocoding);

        // Act
        var result = await handler.Handle(new GetCommuneByCoordinatesQuery(0.0, 0.0), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(GetCommuneByCoordinatesError.InvalidCoordinates);
        await _geocoding.DidNotReceive().FindCommuneAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_commune_not_found_when_geocoding_returns_empty()
    {
        // Arrange
        _geocoding.FindCommuneAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>())
            .Returns(GeocodingOutcome.NotFound());
        var handler = new GetCommuneByCoordinatesHandler(_geocoding);

        // Act
        var result = await handler.Handle(new GetCommuneByCoordinatesQuery(48.8566, 2.3522), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(GetCommuneByCoordinatesError.CommuneNotFound);
    }

    [Fact]
    public async Task Returns_upstream_unavailable_when_geocoding_fails()
    {
        // Arrange
        _geocoding.FindCommuneAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>())
            .Returns(GeocodingOutcome.UpstreamUnavailable());
        var handler = new GetCommuneByCoordinatesHandler(_geocoding);

        // Act
        var result = await handler.Handle(new GetCommuneByCoordinatesQuery(48.8566, 2.3522), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(GetCommuneByCoordinatesError.UpstreamUnavailable);
    }
}
