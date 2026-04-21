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
using VeilleBoisee.Infrastructure.Persistence;

namespace VeilleBoisee.Api.IntegrationTests.Reports;

public class ReportsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ReportsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Returns_201_with_report_id_for_valid_submission()
    {
        // Arrange
        using var client = CreateClient();
        var request = ValidRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/reports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var payload = await response.Content.ReadFromJsonAsync<ReportSubmittedResponse>();
        payload.Should().NotBeNull();
        payload!.ReportId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Returns_400_when_coordinates_are_outside_france()
    {
        // Arrange
        using var client = CreateClient();
        var request = ValidRequest() with { Latitude = 0, Longitude = 0 };

        // Act
        var response = await client.PostAsJsonAsync("/api/reports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns_422_when_description_is_too_short()
    {
        // Arrange
        using var client = CreateClient();
        var request = ValidRequest() with { Description = "Court." };

        // Act
        var response = await client.PostAsJsonAsync("/api/reports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Returns_422_when_email_is_invalid()
    {
        // Arrange
        using var client = CreateClient();
        var request = ValidRequest() with { ContactEmail = "pas-un-email" };

        // Act
        var response = await client.PostAsJsonAsync("/api/reports", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Response_includes_security_headers()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/reports", ValidRequest());

        // Assert
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
    }

    private static SubmitReportRequest ValidRequest() => new(
        Latitude: 48.8566,
        Longitude: 2.3522,
        CommuneInsee: "75056",
        CommuneName: "Paris",
        Description: "Décharge de pneus usagés au bord du chemin forestier, quantité importante.",
        ContactEmail: "contact@exemple.fr");

    private HttpClient CreateClient()
    {
        var encryption = Substitute.For<IEmailEncryptionService>();
        encryption.Encrypt(Arg.Any<string>()).Returns(x => $"encrypted:{x.Arg<string>()}");

        return _factory
            .WithWebHostBuilder(builder =>
            {
                // "Test" environment prevents the EnsureCreatedAsync block in Program.cs from running
                builder.UseEnvironment("Test");
                builder.ConfigureTestServices(services =>
                {
                    // Remove the SqlServer IDbContextOptionsConfiguration<T> entries,
                    // then re-add with InMemory to avoid the two-provider conflict
                    var configType = typeof(IDbContextOptionsConfiguration<VeilleBoiseeDbContext>);
                    var sqlServerConfigs = services.Where(d => d.ServiceType == configType).ToList();
                    foreach (var d in sqlServerConfigs) services.Remove(d);

                    services.RemoveAll<DbContextOptions<VeilleBoiseeDbContext>>();
                    services.AddDbContext<VeilleBoiseeDbContext>(opts =>
                        opts.UseInMemoryDatabase($"test-{Guid.NewGuid()}"));

                    services.RemoveAll<IEmailEncryptionService>();
                    services.AddSingleton(encryption);
                });
            })
            .CreateClient();
    }
}
