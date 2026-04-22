using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Application.Abstractions;

public interface IGeographicEnrichmentService
{
    Task<GeographicEnrichment> EnrichAsync(Coordinates location, CancellationToken cancellationToken);
}

public sealed record GeographicEnrichment(
    string? ParcelleSection,
    string? ParcelleNumero,
    bool IsInForest,
    bool IsInNatura2000Zone)
{
    public static readonly GeographicEnrichment Unavailable = new(null, null, false, false);
}
