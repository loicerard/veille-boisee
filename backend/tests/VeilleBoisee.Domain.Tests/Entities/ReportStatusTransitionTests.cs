using FluentAssertions;
using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.Enums;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Domain.Tests.Entities;

public class ReportStatusTransitionTests
{
    private static readonly Coordinates ParisLocation = Coordinates.Create(48.8566, 2.3522).Value;
    private static readonly CodeInsee ParisInsee = CodeInsee.Create("75056").Value;

    private static Report CreatePendingReport() =>
        Report.Create(ParisLocation, ParisInsee, "Paris", "Décharge de pneus abandonnés.", "encrypted:contact@test.fr");

    [Fact]
    public void Pending_can_transition_to_Routed()
    {
        // Arrange
        var report = CreatePendingReport();

        // Act
        var result = report.UpdateStatus(ReportStatus.Routed);

        // Assert
        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(ReportStatus.Routed);
    }

    [Fact]
    public void Routed_can_transition_to_Acknowledged()
    {
        // Arrange
        var report = CreatePendingReport();
        report.UpdateStatus(ReportStatus.Routed);

        // Act
        var result = report.UpdateStatus(ReportStatus.Acknowledged);

        // Assert
        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(ReportStatus.Acknowledged);
    }

    [Fact]
    public void Acknowledged_can_transition_to_Closed()
    {
        // Arrange
        var report = CreatePendingReport();
        report.UpdateStatus(ReportStatus.Routed);
        report.UpdateStatus(ReportStatus.Acknowledged);

        // Act
        var result = report.UpdateStatus(ReportStatus.Closed);

        // Assert
        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(ReportStatus.Closed);
    }

    [Fact]
    public void Closed_is_terminal_and_cannot_transition()
    {
        // Arrange
        var report = CreatePendingReport();
        report.UpdateStatus(ReportStatus.Routed);
        report.UpdateStatus(ReportStatus.Acknowledged);
        report.UpdateStatus(ReportStatus.Closed);

        // Act
        var result = report.UpdateStatus(ReportStatus.Pending);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReportStatusTransitionError.InvalidTransition);
        report.Status.Should().Be(ReportStatus.Closed);
    }

    [Fact]
    public void Cannot_skip_a_status_step()
    {
        // Arrange
        var report = CreatePendingReport();

        // Act — skip Routed and jump to Acknowledged
        var result = report.UpdateStatus(ReportStatus.Acknowledged);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReportStatusTransitionError.InvalidTransition);
        report.Status.Should().Be(ReportStatus.Pending);
    }

    [Fact]
    public void Cannot_go_backward()
    {
        // Arrange
        var report = CreatePendingReport();
        report.UpdateStatus(ReportStatus.Routed);

        // Act
        var result = report.UpdateStatus(ReportStatus.Pending);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReportStatusTransitionError.InvalidTransition);
        report.Status.Should().Be(ReportStatus.Routed);
    }
}
