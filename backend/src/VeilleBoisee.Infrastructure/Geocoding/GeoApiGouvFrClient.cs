using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.Entities;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Infrastructure.Geocoding;

internal sealed partial class GeoApiGouvFrClient : IGeocodingService
{
    internal const string HttpClientName = "geo-api-gouv-fr";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GeoApiGouvFrClient> _logger;

    [LoggerMessage(Level = LogLevel.Warning, Message = "geo.api.gouv.fr returned {StatusCode} for commune lookup.")]
    private partial void LogUpstreamStatus(HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "geo.api.gouv.fr returned a malformed commune payload.")]
    private partial void LogMalformedPayload();

    [LoggerMessage(Level = LogLevel.Warning, Message = "geo.api.gouv.fr request failed.")]
    private partial void LogRequestFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "geo.api.gouv.fr request timed out.")]
    private partial void LogRequestTimedOut();

    public GeoApiGouvFrClient(HttpClient httpClient, ILogger<GeoApiGouvFrClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GeocodingOutcome> FindCommuneAsync(Coordinates coordinates, CancellationToken cancellationToken)
    {
        var latitude = coordinates.Latitude.ToString("0.######", CultureInfo.InvariantCulture);
        var longitude = coordinates.Longitude.ToString("0.######", CultureInfo.InvariantCulture);
        var requestUri = $"communes?lat={latitude}&lon={longitude}&fields=code,nom&format=json&geometry=centre";

        try
        {
            using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                LogUpstreamStatus(response.StatusCode);
                return GeocodingOutcome.UpstreamUnavailable();
            }

            var payload = await response.Content.ReadFromJsonAsync<CommuneDto[]>(cancellationToken);
            if (payload is null || payload.Length == 0)
            {
                return GeocodingOutcome.NotFound();
            }

            var first = payload[0];
            var codeResult = CodeInsee.Create(first.Code);
            if (codeResult.IsFailure || string.IsNullOrWhiteSpace(first.Name))
            {
                LogMalformedPayload();
                return GeocodingOutcome.UpstreamUnavailable();
            }

            return GeocodingOutcome.Found(Commune.Create(codeResult.Value, first.Name));
        }
        catch (HttpRequestException exception)
        {
            LogRequestFailed(exception);
            return GeocodingOutcome.UpstreamUnavailable();
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LogRequestTimedOut();
            return GeocodingOutcome.UpstreamUnavailable();
        }
    }

    private sealed record CommuneDto(
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("nom")] string? Name);
}
