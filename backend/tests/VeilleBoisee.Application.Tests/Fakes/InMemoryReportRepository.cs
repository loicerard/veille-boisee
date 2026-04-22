using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.Enums;

namespace VeilleBoisee.Application.Tests.Fakes;

internal sealed class InMemoryReportRepository : IReportRepository
{
    private readonly List<Report> _reports = [];

    public IReadOnlyList<Report> Reports => _reports.AsReadOnly();

    public Task AddAsync(Report report, CancellationToken cancellationToken)
    {
        _reports.Add(report);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Report report, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<(IReadOnlyList<Report> Items, int TotalCount)> GetPagedByInseeCodes(
        IReadOnlyList<string> inseeCodes,
        ReportStatus? statusFilter,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var filtered = ApplyFilters(inseeCodes, statusFilter, from, to)
            .OrderByDescending(r => r.SubmittedAt)
            .ToList();

        var items = (IReadOnlyList<Report>)filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((items, filtered.Count));
    }

    public Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_reports.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<Report>> GetAllByInseeCodes(
        IReadOnlyList<string> inseeCodes,
        ReportStatus? statusFilter,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var result = (IReadOnlyList<Report>)ApplyFilters(inseeCodes, statusFilter, from, to)
            .OrderByDescending(r => r.SubmittedAt)
            .ToList();
        return Task.FromResult(result);
    }

    private IEnumerable<Report> ApplyFilters(
        IReadOnlyList<string> inseeCodes,
        ReportStatus? statusFilter,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var query = _reports.Where(r => inseeCodes.Contains(r.CommuneInsee));

        if (statusFilter.HasValue)
            query = query.Where(r => r.Status == statusFilter.Value);

        if (from.HasValue)
            query = query.Where(r => r.SubmittedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(r => r.SubmittedAt <= to.Value);

        return query;
    }
}
