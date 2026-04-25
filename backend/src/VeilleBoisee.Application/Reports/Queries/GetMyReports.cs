using MediatR;
using VeilleBoisee.Application.Abstractions;

namespace VeilleBoisee.Application.Reports.Queries;

public sealed record GetMyReportsQuery(string UserId) : IRequest<IReadOnlyList<MyReportItem>>;

public sealed record MyReportItem(Guid Id, string CommuneName, DateTimeOffset SubmittedAt, string Status);

internal sealed class GetMyReportsHandler : IRequestHandler<GetMyReportsQuery, IReadOnlyList<MyReportItem>>
{
    private readonly IReportRepository _repository;

    public GetMyReportsHandler(IReportRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<MyReportItem>> Handle(
        GetMyReportsQuery request,
        CancellationToken cancellationToken)
    {
        var reports = await _repository.GetBySubmitterUserIdAsync(request.UserId, cancellationToken);

        return reports
            .Select(r => new MyReportItem(r.Id, r.CommuneName, r.SubmittedAt, r.Status.ToString()))
            .ToList();
    }
}
