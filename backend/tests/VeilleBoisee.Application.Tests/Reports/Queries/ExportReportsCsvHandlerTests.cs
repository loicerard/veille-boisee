using System.Text;
using FluentAssertions;
using VeilleBoisee.Application.Reports.Queries;
using VeilleBoisee.Application.Tests.Fakes;
using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Application.Tests.Reports.Queries;

public class ExportReportsCsvHandlerTests
{
    private readonly InMemoryReportRepository _repository = new();
    private readonly IReadOnlyList<string> _inseeCodes = new[] { "75056" };

    private ExportReportsCsvHandler CreateHandler() => new(_repository);

    private static Report CreateReport(string communeInsee = "75056")
    {
        var location = Coordinates.Create(48.8566, 2.3522).Value;
        var insee = CodeInsee.Create(communeInsee).Value;
        return Report.Create(location, insee, "Paris", "Décharge de pneus abandonnés au bord du chemin.", "encrypted:contact@test.fr");
    }

    [Fact]
    public async Task Returns_no_data_when_repository_is_empty()
    {
        // Arrange
        var query = new ExportReportsCsvQuery(_inseeCodes, null, null, null);

        // Act
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExportReportsCsvError.NoData);
    }

    [Fact]
    public async Task Returns_csv_with_header_row()
    {
        // Arrange
        await _repository.AddAsync(CreateReport(), CancellationToken.None);
        var query = new ExportReportsCsvQuery(_inseeCodes, null, null, null);

        // Act
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var csv = Encoding.UTF8.GetString(result.Value.Content);
        csv.Should().StartWith("Id,CommuneName,CommuneInsee,Status,SubmittedAt,IsInForest,IsInNatura2000Zone,ParcelleSection,ParcelleNumero");
    }

    [Fact]
    public async Task Returns_one_data_row_per_report()
    {
        // Arrange
        await _repository.AddAsync(CreateReport(), CancellationToken.None);
        await _repository.AddAsync(CreateReport(), CancellationToken.None);
        var query = new ExportReportsCsvQuery(_inseeCodes, null, null, null);

        // Act
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        // Assert
        var lines = Encoding.UTF8.GetString(result.Value.Content)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(3); // header + 2 data rows
    }

    [Fact]
    public async Task Does_not_include_contact_email_in_csv()
    {
        // Arrange
        await _repository.AddAsync(CreateReport(), CancellationToken.None);
        var query = new ExportReportsCsvQuery(_inseeCodes, null, null, null);

        // Act
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        // Assert
        var csv = Encoding.UTF8.GetString(result.Value.Content);
        csv.Should().NotContain("contact@test.fr");
        csv.Should().NotContain("encrypted:");
    }

    [Fact]
    public async Task Filename_contains_current_date()
    {
        // Arrange
        await _repository.AddAsync(CreateReport(), CancellationToken.None);
        var query = new ExportReportsCsvQuery(_inseeCodes, null, null, null);

        // Act
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        // Assert
        var today = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
        result.Value.FileName.Should().Contain(today);
    }
}
