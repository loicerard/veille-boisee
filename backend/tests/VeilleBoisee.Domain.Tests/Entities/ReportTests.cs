using FluentAssertions;
using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.Enums;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Domain.Tests.Entities;

public class ReportTests
{
    private static readonly Coordinates ParisLocation = Coordinates.Create(48.8566, 2.3522).Value;
    private static readonly CodeInsee ParisInsee = CodeInsee.Create("75056").Value;

    [Fact]
    public void Create_returns_report_with_pending_status_and_new_id()
    {
        // Arrange / Act
        var report = Report.Create(ParisLocation, ParisInsee, "Paris", "Décharge de pneus abandonnés.", "encrypted:contact@test.fr");

        // Assert
        report.Id.Should().NotBeEmpty();
        report.Status.Should().Be(ReportStatus.Pending);
        report.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_stores_location_and_commune()
    {
        // Arrange / Act
        var report = Report.Create(ParisLocation, ParisInsee, "Paris", "Décharge de pneus abandonnés.", "encrypted:contact@test.fr");

        // Assert
        report.Latitude.Should().Be(48.8566);
        report.Longitude.Should().Be(2.3522);
        report.CommuneInsee.Should().Be("75056");
        report.CommuneName.Should().Be("Paris");
    }

    [Fact]
    public void Create_sets_submitted_at_to_utc_now()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var report = Report.Create(ParisLocation, ParisInsee, "Paris", "Décharge de pneus abandonnés.", "encrypted:contact@test.fr");

        // Assert
        report.SubmittedAt.Should().BeOnOrAfter(before);
        report.SubmittedAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_throws_when_commune_name_is_empty()
    {
        // Arrange / Act
        var act = () => Report.Create(ParisLocation, ParisInsee, "", "Décharge de pneus abandonnés.", "encrypted:contact@test.fr");

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
