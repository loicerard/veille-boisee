using MediatR;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Common;
using VeilleBoisee.Domain.Enums;

namespace VeilleBoisee.Application.Reports.Commands;

public sealed record UpdateReportStatusCommand(Guid ReportId, ReportStatus NewStatus)
    : IRequest<Result<ReportStatus, UpdateReportStatusError>>;

public enum UpdateReportStatusError
{
    ReportNotFound,
    InvalidTransition
}

internal sealed class UpdateReportStatusHandler
    : IRequestHandler<UpdateReportStatusCommand, Result<ReportStatus, UpdateReportStatusError>>
{
    private readonly IReportRepository _repository;

    public UpdateReportStatusHandler(IReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ReportStatus, UpdateReportStatusError>> Handle(
        UpdateReportStatusCommand request,
        CancellationToken cancellationToken)
    {
        var report = await _repository.GetByIdAsync(request.ReportId, cancellationToken);

        if (report is null)
            return UpdateReportStatusError.ReportNotFound;

        var result = report.UpdateStatus(request.NewStatus);

        if (result.IsFailure)
            return UpdateReportStatusError.InvalidTransition;

        await _repository.UpdateAsync(report, cancellationToken);

        return result.Value;
    }
}
