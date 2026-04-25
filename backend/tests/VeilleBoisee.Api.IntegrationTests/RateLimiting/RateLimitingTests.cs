using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.ValueObjects;
using VeilleBoisee.Infrastructure.Persistence;

namespace VeilleBoisee.Api.IntegrationTests.RateLimiting;

// Chaque test crée sa propre factory pour garantir un état rate limiter isolé.
public class RateLimitingTests
{
    [Fact]
    public async Task Returns_429_after_exceeding_public_rate_limit()
    {
        // Arrange — limite configurée à 2 pour le test
        await using var factory = BuildFactory(publicLimit: 2);
        using var client = factory.CreateClient();

        // Act — épuisement des 2 permits
        await client.GetAsync(new Uri("/api/communes?lat=48.8566&lon=2.3522", UriKind.Relative));
        await client.GetAsync(new Uri("/api/communes?lat=48.8566&lon=2.3522", UriKind.Relative));
        var response = await client.GetAsync(new Uri("/api/communes?lat=48.8566&lon=2.3522", UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Returns_429_with_retry_after_and_rate_limit_headers()
    {
        // Arrange
        await using var factory = BuildFactory(publicLimit: 2);
        using var client = factory.CreateClient();
        const string communesUrl = "/api/communes?lat=48.8566&lon=2.3522";

        // Act
        await client.GetAsync(new Uri(communesUrl, UriKind.Relative));
        await client.GetAsync(new Uri(communesUrl, UriKind.Relative));
        var response = await client.GetAsync(new Uri(communesUrl, UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.Contains("Retry-After").Should().BeTrue();
        response.Headers.GetValues("X-RateLimit-Remaining").Should().Contain("0");
        response.Headers.Contains("X-RateLimit-Reset").Should().BeTrue();
    }

    [Fact]
    public async Task Returns_429_with_json_body_containing_retry_after()
    {
        // Arrange
        await using var factory = BuildFactory(publicLimit: 2);
        using var client = factory.CreateClient();
        const string communesUrl = "/api/communes?lat=48.8566&lon=2.3522";

        // Act
        await client.GetAsync(new Uri(communesUrl, UriKind.Relative));
        await client.GetAsync(new Uri(communesUrl, UriKind.Relative));
        var response = await client.GetAsync(new Uri(communesUrl, UriKind.Relative));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var body = await response.Content.ReadFromJsonAsync<RateLimitErrorResponse>();
        body.Should().NotBeNull();
        body!.RetryAfter.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Submit_report_rate_limit_applies_before_authorization_check()
    {
        // Arrange — le rate limiter s'applique avant UseAuthorization, donc les requêtes
        // non-authentifiées sont comptées et peuvent déclencher un 429 (pas seulement un 401).
        await using var factory = BuildFactory(submitReportLimit: 2);
        using var client = factory.CreateClient();

        // Act — requêtes sans JWT : les 2 premières reçoivent 401 (auth échoue après rate limit),
        // la 3ème reçoit 429 (rate limit atteint avant que l'auth ne tourne)
        var first = await client.PostAsync(new Uri("/api/reports", UriKind.Relative), null);
        var second = await client.PostAsync(new Uri("/api/reports", UriKind.Relative), null);
        var third = await client.PostAsync(new Uri("/api/reports", UriKind.Relative), null);

        // Assert
        first.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        third.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    private static WebApplicationFactory<Program> BuildFactory(
        int publicLimit = 30,
        int authenticatedLimit = 100,
        int submitReportLimit = 5)
    {
        var geocoding = Substitute.For<IGeocodingService>();
        geocoding.FindCommuneAsync(Arg.Any<Coordinates>(), Arg.Any<CancellationToken>())
            .Returns(GeocodingOutcome.NotFound());

        var encryption = Substitute.For<IEmailEncryptionService>();
        encryption.Encrypt(Arg.Any<string>()).Returns(x => $"encrypted:{x.Arg<string>()}");

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                // "RateLimitingTest" : pas "Test" → UseRateLimiter() est actif,
                // pas "Development" → la base n'est pas supprimée au démarrage.
                builder.UseEnvironment("RateLimitingTest");

                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["RateLimiting:PublicPermitLimit"] = publicLimit.ToString(),
                        ["RateLimiting:AuthenticatedPermitLimit"] = authenticatedLimit.ToString(),
                        ["RateLimiting:SubmitReportPermitLimit"] = submitReportLimit.ToString()
                    }));

                builder.ConfigureTestServices(services =>
                {
                    var configType = typeof(IDbContextOptionsConfiguration<VeilleBoiseeDbContext>);
                    var sqlServerConfigs = services.Where(d => d.ServiceType == configType).ToList();
                    foreach (var d in sqlServerConfigs) services.Remove(d);

                    services.RemoveAll<DbContextOptions<VeilleBoiseeDbContext>>();
                    services.AddDbContext<VeilleBoiseeDbContext>(opts =>
                        opts.UseInMemoryDatabase($"rate-limit-test-{Guid.NewGuid()}"));

                    services.RemoveAll<IGeocodingService>();
                    services.AddSingleton(geocoding);

                    services.RemoveAll<IEmailEncryptionService>();
                    services.AddSingleton(encryption);
                });
            });
    }

    private sealed record RateLimitErrorResponse(int RetryAfter);
}
