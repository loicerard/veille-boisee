using MediatR;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Common;
using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Application.Communes.Queries;

public sealed record GetCommuneByCoordinatesQuery(double Latitude, double Longitude)
    : IRequest<Result<Commune, GetCommuneByCoordinatesError>>;

public enum GetCommuneByCoordinatesError
{
    InvalidCoordinates,
    CommuneNotFound,
    UpstreamUnavailable
}

internal sealed class GetCommuneByCoordinatesHandler
    : IRequestHandler<GetCommuneByCoordinatesQuery, Result<Commune, GetCommuneByCoordinatesError>>
{
    private readonly IGeocodingService _geocoding;

    public GetCommuneByCoordinatesHandler(IGeocodingService geocoding)
    {
        _geocoding = geocoding;
    }

    public async Task<Result<Commune, GetCommuneByCoordinatesError>> Handle(
        GetCommuneByCoordinatesQuery request,
        CancellationToken cancellationToken)
    {
        var coordinatesResult = Coordinates.Create(request.Latitude, request.Longitude);
        if (coordinatesResult.IsFailure)
        {
            return GetCommuneByCoordinatesError.InvalidCoordinates;
        }

        var outcome = await _geocoding.FindCommuneAsync(coordinatesResult.Value, cancellationToken);

        return outcome.Status switch
        {
            GeocodingStatus.Found => outcome.Commune!,
            GeocodingStatus.NotFound => GetCommuneByCoordinatesError.CommuneNotFound,
            GeocodingStatus.UpstreamUnavailable => GetCommuneByCoordinatesError.UpstreamUnavailable,
            _ => GetCommuneByCoordinatesError.UpstreamUnavailable
        };
    }
}
