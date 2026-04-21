using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Entities;
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
}
