using MediatR;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Common;

namespace VeilleBoisee.Application.Reports.Queries;

public sealed record GetReportPhotoQuery(Guid ReportId)
    : IRequest<Result<ReportPhotoDto, GetReportPhotoError>>;

public enum GetReportPhotoError
{
    NotFound,
    NoPhoto
}

public sealed record ReportPhotoDto(byte[] Data, string MimeType);

internal sealed class GetReportPhotoHandler
    : IRequestHandler<GetReportPhotoQuery, Result<ReportPhotoDto, GetReportPhotoError>>
{
    private readonly IReportRepository _repository;

    public GetReportPhotoHandler(IReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ReportPhotoDto, GetReportPhotoError>> Handle(
        GetReportPhotoQuery request,
        CancellationToken cancellationToken)
    {
        var report = await _repository.GetByIdAsync(request.ReportId, cancellationToken);

        if (report is null)
            return GetReportPhotoError.NotFound;

        if (report.PhotoData is null || report.PhotoMimeType is null)
            return GetReportPhotoError.NoPhoto;

        return new ReportPhotoDto(report.PhotoData, report.PhotoMimeType);
    }
}
