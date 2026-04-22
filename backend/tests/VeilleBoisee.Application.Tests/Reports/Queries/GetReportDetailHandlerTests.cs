using FluentAssertions;
using VeilleBoisee.Application.Reports.Queries;
using VeilleBoisee.Application.Tests.Fakes;
using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Application.Tests.Reports.Queries;

public class GetReportDetailHandlerTests
{
    private readonly InMemoryReportRepository _repository = new();
    private readonly FakeEmailEncryptionService _encryption = new();

    private GetReportDetailHandler CreateHandler() => new(_repository, _encryption);

    private static Report CreateReport()
    {
        var location = Coordinates.Create(48.8566, 2.3522).Value;
        var insee = CodeInsee.Create("75056").Value;
        return Report.Create(location, insee, "Paris", "Décharge de pneus abandonnés au bord du chemin.", "encrypted:contact@test.fr");
    }

    [Fact]
    public async Task Returns_detail_with_decrypted_email_when_report_exists()
    {
        // Arrange
        var report = CreateReport();
        await _repository.AddAsync(report, CancellationToken.None);

        // Act
        var result = await CreateHandler().Handle(new GetReportDetailQuery(report.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(report.Id);
        result.Value.ContactEmail.Should().Be("contact@test.fr");
    }

    [Fact]
    public async Task Returns_not_found_when_report_does_not_exist()
    {
        // Arrange
        var query = new GetReportDetailQuery(Guid.NewGuid());

        // Act
        var result = await CreateHandler().Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(GetReportDetailError.NotFound);
    }

    [Fact]
    public async Task Returns_all_report_fields()
    {
        // Arrange
        var report = CreateReport();
        report.Enrich("ZK", "1234", true, false);
        await _repository.AddAsync(report, CancellationToken.None);

        // Act
        var result = await CreateHandler().Handle(new GetReportDetailQuery(report.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Latitude.Should().Be(48.8566);
        dto.Longitude.Should().Be(2.3522);
        dto.CommuneInsee.Should().Be("75056");
        dto.CommuneName.Should().Be("Paris");
        dto.ParcelleSection.Should().Be("ZK");
        dto.ParcelleNumero.Should().Be("1234");
        dto.IsInForest.Should().BeTrue();
        dto.IsInNatura2000Zone.Should().BeFalse();
    }
}
