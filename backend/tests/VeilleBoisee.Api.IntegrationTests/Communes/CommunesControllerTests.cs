using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using VeilleBoisee.Api.Controllers;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.ValueObjects;
using VeilleBoisee.Infrastructure.Persistence;

namespace VeilleBoisee.Api.IntegrationTests.Communes;

public class CommunesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CommunesControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Returns_200_with_commune_when_upstream_finds_one()
    {
        // Arrange
        var paris = Commune.Create(CodeInsee.Create("75056").Value, "Paris");
        var geocoding = Substitute.For<IGeocodingService>();
        geocoding.FindCommuneAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>())
            .Returns(GeocodingOutcome.Found(paris));
        using var client = CreateClientWith(geocoding);

        // Act
        var response = await client.GetAsync(new Uri("/api/communes?lat=48.8566&lon=2.3522", UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<CommuneResponse>();
        payload.Should().NotBeNull();
        payload!.CodeInsee.Should().Be("75056");
        payload.Name.Should().Be("Paris");
    }

    [Fact]
    public async Task Returns_400_when_coordinates_are_outside_france()
    {
        using var client = CreateClientWith(Substitute.For<IGeocodingService>());

        var response = await client.GetAsync(new Uri("/api/communes?lat=0&lon=0", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns_404_when_upstream_finds_nothing()
    {
        var geocoding = Substitute.For<IGeocodingService>();
        geocoding.FindCommuneAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>())
            .Returns(GeocodingOutcome.NotFound());
        using var client = CreateClientWith(geocoding);

        var response = await client.GetAsync(new Uri("/api/communes?lat=48.8566&lon=2.3522", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_502_when_upstream_is_unavailable()
    {
        var geocoding = Substitute.For<IGeocodingService>();
        geocoding.FindCommuneAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>())
            .Returns(GeocodingOutcome.UpstreamUnavailable());
        using var client = CreateClientWith(geocoding);

        var response = await client.GetAsync(new Uri("/api/communes?lat=48.8566&lon=2.3522", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task Response_includes_security_headers()
    {
        var geocoding = Substitute.For<IGeocodingService>();
        geocoding.FindCommuneAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>())
            .Returns(GeocodingOutcome.NotFound());
        using var client = CreateClientWith(geocoding);

        var response = await client.GetAsync(new Uri("/api/communes?lat=48.8566&lon=2.3522", UriKind.Relative));

        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
    }

    private HttpClient CreateClientWith(IGeocodingService geocoding)
    {
        return _factory
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test");
                builder.ConfigureTestServices(services =>
                {
                    var configType = typeof(IDbContextOptionsConfiguration<VeilleBoiseeDbContext>);
                    var sqlServerConfigs = services.Where(d => d.ServiceType == configType).ToList();
                    foreach (var d in sqlServerConfigs) services.Remove(d);

                    services.RemoveAll<DbContextOptions<VeilleBoiseeDbContext>>();
                    services.AddDbContext<VeilleBoiseeDbContext>(opts =>
                        opts.UseInMemoryDatabase($"test-{Guid.NewGuid()}"));

                    services.RemoveAll<IGeocodingService>();
                    services.AddSingleton(geocoding);
                });
            })
            .CreateClient();
    }
}
