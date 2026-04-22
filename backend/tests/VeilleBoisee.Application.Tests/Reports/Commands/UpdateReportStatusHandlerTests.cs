using FluentAssertions;
using VeilleBoisee.Application.Reports.Commands;
using VeilleBoisee.Application.Tests.Fakes;
using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.Enums;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Application.Tests.Reports.Commands;

public class UpdateReportStatusHandlerTests
{
    private readonly InMemoryReportRepository _repository = new();

    private UpdateReportStatusHandler CreateHandler() => new(_repository);

    private static Report CreateReport()
    {
        var location = Coordinates.Create(48.8566, 2.3522).Value;
        var insee = CodeInsee.Create("75056").Value;
        return Report.Create(location, insee, "Paris", "Décharge de pneus abandonnés au bord du chemin.", "encrypted:contact@test.fr");
    }

    [Fact]
    public async Task Returns_new_status_for_valid_transition_pending_to_routed()
    {
        // Arrange
        var report = CreateReport();
        await _repository.AddAsync(report, CancellationToken.None);
        var command = new UpdateReportStatusCommand(report.Id, ReportStatus.Routed);

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(ReportStatus.Routed);
    }

    [Fact]
    public async Task Returns_report_not_found_when_id_is_unknown()
    {
        // Arrange
        var command = new UpdateReportStatusCommand(Guid.NewGuid(), ReportStatus.Routed);

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UpdateReportStatusError.ReportNotFound);
    }

    [Fact]
    public async Task Returns_invalid_transition_for_illegal_status_change()
    {
        // Arrange
        var report = CreateReport();
        await _repository.AddAsync(report, CancellationToken.None);
        var command = new UpdateReportStatusCommand(report.Id, ReportStatus.Closed);

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UpdateReportStatusError.InvalidTransition);
    }

    [Fact]
    public async Task Updates_report_status_in_repository_on_success()
    {
        // Arrange
        var report = CreateReport();
        await _repository.AddAsync(report, CancellationToken.None);
        var command = new UpdateReportStatusCommand(report.Id, ReportStatus.Routed);

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        var stored = await _repository.GetByIdAsync(report.Id, CancellationToken.None);
        stored!.Status.Should().Be(ReportStatus.Routed);
    }

    [Fact]
    public async Task Does_not_change_status_on_invalid_transition()
    {
        // Arrange
        var report = CreateReport();
        await _repository.AddAsync(report, CancellationToken.None);
        var command = new UpdateReportStatusCommand(report.Id, ReportStatus.Acknowledged);

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        var stored = await _repository.GetByIdAsync(report.Id, CancellationToken.None);
        stored!.Status.Should().Be(ReportStatus.Pending);
    }
}
