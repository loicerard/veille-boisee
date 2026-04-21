using VeilleBoisee.Domain.Entities;

namespace VeilleBoisee.Application.Abstractions;

public interface IReportRepository
{
    Task AddAsync(Report report, CancellationToken cancellationToken);
    Task UpdateAsync(Report report, CancellationToken cancellationToken);
}
