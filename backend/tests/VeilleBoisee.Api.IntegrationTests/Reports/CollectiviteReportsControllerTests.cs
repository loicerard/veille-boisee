using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Application.Reports.Queries;
using VeilleBoisee.Infrastructure.Persistence;

namespace VeilleBoisee.Api.IntegrationTests.Reports;

public class CollectiviteReportsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CollectiviteReportsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_returns_200_with_empty_page_when_no_reports_exist()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/collectivite/reports");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<ReportPageDto>();
        page.Should().NotBeNull();
        page!.TotalCount.Should().Be(0);
        page.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task List_returns_200_with_reports_matching_collectivite_insee_codes()
    {
        // Arrange
        using var client = CreateClient();
        await SeedReport(client);

        // Act
        var response = await client.GetAsync("/api/collectivite/reports");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<ReportPageDto>();
        page!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Detail_returns_404_for_unknown_report_id()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync($"/api/collectivite/reports/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Detail_returns_200_with_decrypted_email()
    {
        // Arrange
        using var client = CreateClient();
        var reportId = await SeedReport(client);

        // Act
        var response = await client.GetAsync($"/api/collectivite/reports/{reportId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("contactEmail").GetString().Should().Be("contact@exemple.fr");
    }

    [Fact]
    public async Task UpdateStatus_returns_200_for_valid_transition_pending_to_routed()
    {
        // Arrange
        using var client = CreateClient();
        var reportId = await SeedReport(client);

        // Act
        var response = await client.PatchAsJsonAsync(
            $"/api/collectivite/reports/{reportId}/status",
            new { newStatus = 1 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("status").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task UpdateStatus_returns_422_for_invalid_transition()
    {
        // Arrange
        using var client = CreateClient();
        var reportId = await SeedReport(client);

        // Act — try to jump directly to Closed from Pending
        var response = await client.PatchAsJsonAsync(
            $"/api/collectivite/reports/{reportId}/status",
            new { newStatus = 3 });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ExportCsv_returns_csv_file_with_correct_content_type()
    {
        // Arrange
        using var client = CreateClient();
        await SeedReport(client);

        // Act
        var response = await client.GetAsync("/api/collectivite/reports/export.csv");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task ExportCsv_returns_204_when_no_matching_reports()
    {
        // Arrange
        using var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/collectivite/reports/export.csv");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private async Task<Guid> SeedReport(HttpClient client)
    {
        var request = new
        {
            latitude = 48.8566,
            longitude = 2.3522,
            communeInsee = "75056",
            communeName = "Paris",
            description = "Décharge de pneus usagés au bord du chemin forestier, quantité importante.",
            contactEmail = "contact@exemple.fr"
        };
        var response = await client.PostAsJsonAsync("/api/reports", request);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("reportId").GetGuid();
    }

    private HttpClient CreateClient()
    {
        var encryption = Substitute.For<IEmailEncryptionService>();
        encryption.Encrypt(Arg.Any<string>()).Returns(x => $"encrypted:{x.Arg<string>()}");
        encryption.Decrypt(Arg.Any<string>()).Returns(x =>
        {
            var ciphertext = x.Arg<string>();
            return ciphertext.StartsWith("encrypted:", StringComparison.Ordinal)
                ? ciphertext["encrypted:".Length..]
                : ciphertext;
        });

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
                    var dbName = $"test-{Guid.NewGuid()}";
                    services.AddDbContext<VeilleBoiseeDbContext>(opts =>
                        opts.UseInMemoryDatabase(dbName));

                    services.RemoveAll<IEmailEncryptionService>();
                    services.AddSingleton(encryption);
                });
            })
            .CreateClient();
    }
}
