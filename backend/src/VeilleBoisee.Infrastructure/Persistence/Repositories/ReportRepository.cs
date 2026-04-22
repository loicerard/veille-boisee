using Microsoft.EntityFrameworkCore;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.Enums;
using VeilleBoisee.Infrastructure.Persistence;

namespace VeilleBoisee.Infrastructure.Persistence.Repositories;

internal sealed class ReportRepository : IReportRepository
{
    private readonly VeilleBoiseeDbContext _context;

    public ReportRepository(VeilleBoiseeDbContext context) => _context = context;

    public async Task AddAsync(Report report, CancellationToken cancellationToken)
    {
        await _context.Reports.AddAsync(report, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Report report, CancellationToken cancellationToken)
    {
        // Entity is already tracked from AddAsync; change tracker detects the mutation
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Report> Items, int TotalCount)> GetPagedByInseeCodes(
        IReadOnlyList<string> inseeCodes,
        ReportStatus? statusFilter,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = BuildFilterQuery(inseeCodes, statusFilter, from, to);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.Reports.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Report>> GetAllByInseeCodes(
        IReadOnlyList<string> inseeCodes,
        ReportStatus? statusFilter,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        return await BuildFilterQuery(inseeCodes, statusFilter, from, to)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Report> BuildFilterQuery(
        IReadOnlyList<string> inseeCodes,
        ReportStatus? statusFilter,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        var query = inseeCodes.Count == 0
            ? _context.Reports.AsQueryable()
            : _context.Reports.Where(r => inseeCodes.Contains(r.CommuneInsee));

        if (statusFilter.HasValue)
            query = query.Where(r => r.Status == statusFilter.Value);

        if (from.HasValue)
            query = query.Where(r => r.SubmittedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(r => r.SubmittedAt <= to.Value);

        return query;
    }
}
