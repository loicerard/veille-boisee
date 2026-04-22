using MediatR;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Common;
using VeilleBoisee.Domain.Enums;

namespace VeilleBoisee.Application.Reports.Queries;

public sealed record GetReportDetailQuery(Guid ReportId)
    : IRequest<Result<ReportDetailDto, GetReportDetailError>>;

public enum GetReportDetailError
{
    NotFound
}

public sealed record ReportDetailDto(
    Guid Id,
    double Latitude,
    double Longitude,
    string CommuneInsee,
    string CommuneName,
    string Description,
    string ContactEmail,
    ReportStatus Status,
    DateTimeOffset SubmittedAt,
    string? ParcelleSection,
    string? ParcelleNumero,
    bool? IsInForest,
    bool? IsInNatura2000Zone,
    bool HasPhoto);

internal sealed class GetReportDetailHandler
    : IRequestHandler<GetReportDetailQuery, Result<ReportDetailDto, GetReportDetailError>>
{
    private readonly IReportRepository _repository;
    private readonly IEmailEncryptionService _encryption;

    public GetReportDetailHandler(IReportRepository repository, IEmailEncryptionService encryption)
    {
        _repository = repository;
        _encryption = encryption;
    }

    public async Task<Result<ReportDetailDto, GetReportDetailError>> Handle(
        GetReportDetailQuery request,
        CancellationToken cancellationToken)
    {
        var report = await _repository.GetByIdAsync(request.ReportId, cancellationToken);

        if (report is null)
            return GetReportDetailError.NotFound;

        var contactEmail = _encryption.Decrypt(report.EncryptedContactEmail);

        return new ReportDetailDto(
            report.Id,
            report.Latitude,
            report.Longitude,
            report.CommuneInsee,
            report.CommuneName,
            report.Description,
            contactEmail,
            report.Status,
            report.SubmittedAt,
            report.ParcelleSection,
            report.ParcelleNumero,
            report.IsInForest,
            report.IsInNatura2000Zone,
            report.PhotoData is not null);
    }
}
