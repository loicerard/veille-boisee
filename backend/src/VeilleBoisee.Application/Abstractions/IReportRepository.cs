using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.Enums;

namespace VeilleBoisee.Application.Abstractions;

public interface IReportRepository
{
    Task AddAsync(Report report, CancellationToken cancellationToken);
    Task UpdateAsync(Report report, CancellationToken cancellationToken);

    Task<(IReadOnlyList<Report> Items, int TotalCount)> GetPagedByInseeCodes(
        IReadOnlyList<string> inseeCodes,
        ReportStatus? statusFilter,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Report?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Report>> GetBySubmitterUserIdAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Report>> GetAllByInseeCodes(
        IReadOnlyList<string> inseeCodes,
        ReportStatus? statusFilter,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken);
}
