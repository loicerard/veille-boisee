using FluentAssertions;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Application.Reports.Commands;
using VeilleBoisee.Application.Tests.Fakes;
using VeilleBoisee.Domain.Entities;

namespace VeilleBoisee.Application.Tests.Reports.Commands;

public class SubmitReportHandlerTests
{
    private readonly InMemoryReportRepository _repository = new();
    private readonly FakeEmailEncryptionService _encryption = new();
    private readonly FakeGeographicEnrichmentService _enrichment = new();
    private readonly FakeExifStrippingService _exifStripping = new();

    private SubmitReportHandler CreateHandler() => new(_repository, _encryption, _enrichment, _exifStripping);

    private static SubmitReportCommand ValidCommand(
        string description = "Décharge de pneus usagés au bord du chemin forestier, quantité importante.",
        string email = "contact@exemple.fr") =>
        new(48.8566, 2.3522, "75056", "Paris", description, email);

    [Fact]
    public async Task Persists_report_and_returns_its_id_for_valid_submission()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repository.Reports.Should().HaveCount(1);
        _repository.Reports[0].Id.Should().Be(result.Value);
    }

    [Fact]
    public async Task Stores_encrypted_email_not_plaintext()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        await handler.Handle(ValidCommand(email: "secret@test.fr"), CancellationToken.None);

        // Assert
        _repository.Reports[0].EncryptedContactEmail.Should().Be("encrypted:secret@test.fr");
    }

    [Fact]
    public async Task Stores_geographic_enrichment_on_report()
    {
        // Arrange
        _enrichment.NextResult = new GeographicEnrichment("ZK", "1234", true, true);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = _repository.Reports[0];
        report.ParcelleSection.Should().Be("ZK");
        report.ParcelleNumero.Should().Be("1234");
        report.IsInForest.Should().BeTrue();
        report.IsInNatura2000Zone.Should().BeTrue();
    }

    [Fact]
    public async Task Succeeds_even_when_enrichment_is_unavailable()
    {
        // Arrange
        _enrichment.NextResult = GeographicEnrichment.Unavailable;
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = _repository.Reports[0];
        report.ParcelleSection.Should().BeNull();
        report.ParcelleNumero.Should().BeNull();
        report.IsInForest.Should().BeFalse();
        report.IsInNatura2000Zone.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_invalid_coordinates_for_coordinates_outside_france()
    {
        // Arrange
        var command = new SubmitReportCommand(0, 0, "75056", "Paris", ValidCommand().Description, ValidCommand().ContactEmail);

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SubmitReportError.InvalidCoordinates);
    }

    [Fact]
    public async Task Returns_invalid_commune_for_malformed_insee_code()
    {
        // Arrange
        var command = ValidCommand() with { CommuneInsee = "XXXXX" };

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SubmitReportError.InvalidCommuneInsee);
    }

    [Fact]
    public async Task Returns_invalid_description_when_too_short()
    {
        // Arrange
        var command = ValidCommand(description: "Court.");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SubmitReportError.InvalidDescription);
    }

    [Fact]
    public async Task Returns_invalid_description_when_too_long()
    {
        // Arrange
        var command = ValidCommand(description: new string('a', Report.DescriptionMaxLength + 1));

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SubmitReportError.InvalidDescription);
    }

    [Fact]
    public async Task Returns_invalid_email_for_malformed_email()
    {
        // Arrange
        var command = ValidCommand(email: "pas-un-email");

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SubmitReportError.InvalidContactEmail);
    }

    [Fact]
    public async Task Does_not_persist_when_validation_fails()
    {
        // Arrange
        var command = new SubmitReportCommand(0, 0, "75056", "Paris", ValidCommand().Description, ValidCommand().ContactEmail);

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        _repository.Reports.Should().BeEmpty();
    }

    [Fact]
    public async Task Persists_photo_bytes_when_photo_is_provided()
    {
        // Arrange
        var photoBytes = new byte[] { 0xFF, 0xD8, 0xFF };
        using var photoStream = new MemoryStream(photoBytes);
        var command = ValidCommand() with { PhotoStream = photoStream, PhotoMimeType = "image/jpeg" };

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = _repository.Reports[0];
        report.PhotoData.Should().BeEquivalentTo(photoBytes);
        report.PhotoMimeType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task Persists_no_photo_when_photo_is_not_provided()
    {
        // Arrange
        var command = ValidCommand();

        // Act
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var report = _repository.Reports[0];
        report.PhotoData.Should().BeNull();
        report.PhotoMimeType.Should().BeNull();
    }
}
