using MediatR;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Common;
using VeilleBoisee.Domain.Enums;

namespace VeilleBoisee.Application.Reports.Queries;

public sealed record GetReportsByCollectiviteQuery(
    IReadOnlyList<string> InseeCodes,
    ReportStatus? StatusFilter,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page,
    int PageSize
) : IRequest<Result<ReportPageDto, GetReportsByCollectiviteError>>;

public enum GetReportsByCollectiviteError
{
    InvalidParameters
}

public sealed record ReportPageDto(
    IReadOnlyList<ReportSummaryDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ReportSummaryDto(
    Guid Id,
    string CommuneName,
    string CommuneInsee,
    ReportStatus Status,
    DateTimeOffset SubmittedAt,
    bool IsInForest,
    bool IsInNatura2000Zone);

internal sealed class GetReportsByCollectiviteHandler
    : IRequestHandler<GetReportsByCollectiviteQuery, Result<ReportPageDto, GetReportsByCollectiviteError>>
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    private readonly IReportRepository _repository;

    public GetReportsByCollectiviteHandler(IReportRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ReportPageDto, GetReportsByCollectiviteError>> Handle(
        GetReportsByCollectiviteQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize < 1 || request.PageSize > MaxPageSize)
            return GetReportsByCollectiviteError.InvalidParameters;

        var (items, total) = await _repository.GetPagedByInseeCodes(
            request.InseeCodes,
            request.StatusFilter,
            request.From,
            request.To,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items
            .Select(r => new ReportSummaryDto(
                r.Id,
                r.CommuneName,
                r.CommuneInsee,
                r.Status,
                r.SubmittedAt,
                r.IsInForest ?? false,
                r.IsInNatura2000Zone ?? false))
            .ToList();

        return new ReportPageDto(dtos, total, request.Page, request.PageSize);
    }
}
