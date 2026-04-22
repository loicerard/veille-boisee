using MediatR;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Common;
using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Application.Reports.Commands;

public sealed record SubmitReportCommand(
    double Latitude,
    double Longitude,
    string CommuneInsee,
    string CommuneName,
    string Description,
    string ContactEmail,
    Stream? PhotoStream = null,
    string? PhotoMimeType = null
) : IRequest<Result<Guid, SubmitReportError>>;

public enum SubmitReportError
{
    InvalidCoordinates,
    InvalidCommuneInsee,
    InvalidDescription,
    InvalidContactEmail
}

internal sealed class SubmitReportHandler
    : IRequestHandler<SubmitReportCommand, Result<Guid, SubmitReportError>>
{
    private readonly IReportRepository _repository;
    private readonly IEmailEncryptionService _encryption;
    private readonly IGeographicEnrichmentService _enrichment;
    private readonly IExifStrippingService _exifStripping;

    public SubmitReportHandler(
        IReportRepository repository,
        IEmailEncryptionService encryption,
        IGeographicEnrichmentService enrichment,
        IExifStrippingService exifStripping)
    {
        _repository = repository;
        _encryption = encryption;
        _enrichment = enrichment;
        _exifStripping = exifStripping;
    }

    public async Task<Result<Guid, SubmitReportError>> Handle(
        SubmitReportCommand request,
        CancellationToken cancellationToken)
    {
        var coordinatesResult = Coordinates.Create(request.Latitude, request.Longitude);
        if (coordinatesResult.IsFailure)
            return SubmitReportError.InvalidCoordinates;

        var codeInseeResult = CodeInsee.Create(request.CommuneInsee);
        if (codeInseeResult.IsFailure)
            return SubmitReportError.InvalidCommuneInsee;

        if (string.IsNullOrWhiteSpace(request.Description)
            || request.Description.Length < Report.DescriptionMinLength
            || request.Description.Length > Report.DescriptionMaxLength)
            return SubmitReportError.InvalidDescription;

        var emailResult = ContactEmail.Create(request.ContactEmail);
        if (emailResult.IsFailure)
            return SubmitReportError.InvalidContactEmail;

        var encryptedEmail = _encryption.Encrypt(emailResult.Value.Value);

        byte[]? photoData = null;
        if (request.PhotoStream is not null)
            photoData = await _exifStripping.StripExifAsync(request.PhotoStream, cancellationToken);

        var report = Report.Create(
            coordinatesResult.Value,
            codeInseeResult.Value,
            request.CommuneName,
            request.Description,
            encryptedEmail,
            photoData,
            request.PhotoMimeType);

        await _repository.AddAsync(report, cancellationToken);

        var geo = await _enrichment.EnrichAsync(coordinatesResult.Value, cancellationToken);
        report.Enrich(geo.ParcelleSection, geo.ParcelleNumero, geo.IsInForest, geo.IsInNatura2000Zone);
        await _repository.UpdateAsync(report, cancellationToken);

        return report.Id;
    }
}
