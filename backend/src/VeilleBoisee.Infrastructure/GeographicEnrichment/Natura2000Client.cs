using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Infrastructure.Enrichment;

internal sealed partial class Natura2000Client
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Natura2000Client> _logger;

    [LoggerMessage(Level = LogLevel.Debug, Message = "Natura2000 request: {BaseAddress}{Url}")]
    private partial void LogNaturaRequest(Uri? baseAddress, string url);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Natura2000 response ({StatusCode}): {Body}")]
    private partial void LogNaturaResponse(HttpStatusCode statusCode, string body);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Natura2000 zone check result: {IsInZone}")]
    private partial void LogNaturaResult(bool isInZone);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API Carto Natura2000 returned {StatusCode}.")]
    private partial void LogUpstreamStatus(HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API Carto Natura2000 request failed.")]
    private partial void LogRequestFailed(Exception exception);

    public Natura2000Client(HttpClient httpClient, ILogger<Natura2000Client> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> IsInNatura2000ZoneAsync(Coordinates location, CancellationToken cancellationToken)
    {
        var lon = location.Longitude.ToString("0.######", CultureInfo.InvariantCulture);
        var lat = location.Latitude.ToString("0.######", CultureInfo.InvariantCulture);
        // API Carto nature module expects a GeoJSON geometry, not raw lon/lat
        var geom = $$$"""{"type":"Point","coordinates":[{{{lon}}},{{{lat}}}]}""";
        var url = $"nature/natura-habitat?geom={Uri.EscapeDataString(geom)}";
        LogNaturaRequest(_httpClient.BaseAddress, url);

        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            LogNaturaResponse(response.StatusCode, Truncate(body));

            if (!response.IsSuccessStatusCode)
            {
                LogUpstreamStatus(response.StatusCode);
                return false;
            }

            var fc = JsonSerializer.Deserialize<Natura2000Result>(body);
            var isInZone = (fc?.NumberReturned ?? 0) > 0;
            LogNaturaResult(isInZone);
            return isInZone;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            LogRequestFailed(ex);
            return false;
        }
    }

    private static string Truncate(string s) => s.Length > 1000 ? s[..1000] + "…" : s;

    private sealed record Natura2000Result(
        [property: JsonPropertyName("numberReturned")] int NumberReturned);
}
