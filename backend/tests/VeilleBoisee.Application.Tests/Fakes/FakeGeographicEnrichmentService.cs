using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Application.Tests.Fakes;

internal sealed class FakeGeographicEnrichmentService : IGeographicEnrichmentService
{
    public GeographicEnrichment NextResult { get; set; } = new("AB", "0042", true, false);

    public Task<GeographicEnrichment> EnrichAsync(Coordinates location, CancellationToken cancellationToken)
        => Task.FromResult(NextResult);
}
