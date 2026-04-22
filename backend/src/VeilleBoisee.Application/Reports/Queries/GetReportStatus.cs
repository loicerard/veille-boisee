using MediatR;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Common;
using VeilleBoisee.Domain.Enums;

namespace VeilleBoisee.Application.Reports.Queries;

public sealed record GetReportStatusQuery(Guid ReportId)
    : IRequest<Result<ReportStatus, GetReportStatusError>>;

public enum GetReportStatusError { NotFound }

internal sealed class GetReportStatusHandler
    : IRequestHandler<GetReportStatusQuery, Result<ReportStatus, GetReportStatusError>>
{
    private readonly IReportRepository _repository;

    public GetReportStatusHandler(IReportRepository repository)
        => _repository = repository;

    public async Task<Result<ReportStatus, GetReportStatusError>> Handle(
        GetReportStatusQuery request,
        CancellationToken cancellationToken)
    {
        var report = await _repository.GetByIdAsync(request.ReportId, cancellationToken);

        if (report is null)
            return GetReportStatusError.NotFound;

        return report.Status;
    }
}
