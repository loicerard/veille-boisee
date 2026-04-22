using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Application.Abstractions;

public interface IGeocodingService
{
    Task<GeocodingOutcome> FindCommuneAsync(Coordinates coordinates, CancellationToken cancellationToken);
}

public enum GeocodingStatus
{
    Found,
    NotFound,
    UpstreamUnavailable
}

public sealed record GeocodingOutcome(GeocodingStatus Status, Commune? Commune)
{
    public static GeocodingOutcome Found(Commune commune) => new(GeocodingStatus.Found, commune);
    public static GeocodingOutcome NotFound() => new(GeocodingStatus.NotFound, null);
    public static GeocodingOutcome UpstreamUnavailable() => new(GeocodingStatus.UpstreamUnavailable, null);
}
