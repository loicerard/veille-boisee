using FluentAssertions;
using VeilleBoisee.Application.Reports.Queries;
using VeilleBoisee.Application.Tests.Fakes;
using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.Enums;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Application.Tests.Reports.Queries;

public class GetReportsByCollectiviteHandlerTests
{
    private readonly InMemoryReportRepository _repository = new();
    private readonly IReadOnlyList<string> _inseeCodes = new[] { "75056", "75101" };

    private GetReportsByCollectiviteHandler CreateHandler() => new(_repository);

    private static Report CreateReport(string communeInsee = "75056")
    {
        var location = Coordinates.Create(48.8566, 2.3522).Value;
        var insee = CodeInsee.Create(communeInsee).Value;
        return Report.Create(location, insee, "Paris", "Décharge de pneus abandonnés au bord du chemin.", "encrypted:contact@test.fr");
    }

    [Fact]
    public async Task Returns_page_of_reports_when_repository_has_matching_data()
    {
        // Arrange
        await _repository.AddAsync(CreateReport("75056"), CancellationToken.None);
        var query = new GetReportsByCollectiviteQuery(_inseeCodes, null, null, null, 1, 20);

        // Act
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Returns_empty_page_when_no_matching_reports()
    {
        // Arrange
        var query = new GetReportsByCollectiviteQuery(_inseeCodes, null, null, null, 1, 20);

        // Act
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Returns_invalid_parameters_when_page_is_less_than_one()
    {
        // Arrange
        var query = new GetReportsByCollectiviteQuery(_inseeCodes, null, null, null, 0, 20);

        // Act
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(GetReportsByCollectiviteError.InvalidParameters);
    }

    [Fact]
    public async Task Returns_invalid_parameters_when_page_size_exceeds_maximum()
    {
        // Arrange
        var query = new GetReportsByCollectiviteQuery(_inseeCodes, null, null, null, 1, 101);

        // Act
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(GetReportsByCollectiviteError.InvalidParameters);
    }

    [Fact]
    public async Task Filters_by_status_when_provided()
    {
        // Arrange
        var report = CreateReport("75056");
        report.UpdateStatus(ReportStatus.Routed);
        await _repository.AddAsync(report, CancellationToken.None);
        await _repository.AddAsync(CreateReport("75056"), CancellationToken.None);

        var query = new GetReportsByCollectiviteQuery(_inseeCodes, ReportStatus.Pending, null, null, 1, 20);

        // Act
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Status.Should().Be(ReportStatus.Pending);
    }

    [Fact]
    public async Task Does_not_include_reports_from_other_communes()
    {
        // Arrange
        await _repository.AddAsync(CreateReport("01001"), CancellationToken.None);
        var query = new GetReportsByCollectiviteQuery(_inseeCodes, null, null, null, 1, 20);

        // Act
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }
}
