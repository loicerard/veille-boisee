using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Entities;

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
}
