using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Infrastructure.Enrichment;

internal sealed partial class GeoplatformeWfsClient
{
    private const string ParcelleLayer = "BDPARCELLAIRE-VECTEUR_WLD_BDD_WGS84G:parcelle";
    private const string ForetLayer = "IGNF_MASQUE-FORET.2021-2023:masque_foret";

    private readonly HttpClient _httpClient;
    private readonly ILogger<GeoplatformeWfsClient> _logger;

    [LoggerMessage(Level = LogLevel.Debug, Message = "WFS request: layer={Layer} url={BaseAddress}{Url}")]
    private partial void LogWfsRequest(string layer, Uri? baseAddress, string url);

    [LoggerMessage(Level = LogLevel.Debug, Message = "WFS response ({StatusCode}) layer={Layer}: {Body}")]
    private partial void LogWfsResponse(HttpStatusCode statusCode, string layer, string body);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Parcelle result: section={Section} numero={Numero}")]
    private partial void LogParcelleResult(string? section, string? numero);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Forest check result: {IsInForest}")]
    private partial void LogForestResult(bool isInForest);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Géoplateforme WFS returned {StatusCode} for layer {Layer}.")]
    private partial void LogUpstreamStatus(HttpStatusCode statusCode, string layer);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Géoplateforme WFS request failed for layer {Layer}.")]
    private partial void LogRequestFailed(string layer, Exception exception);

    public GeoplatformeWfsClient(HttpClient httpClient, ILogger<GeoplatformeWfsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(string? Section, string? Numero)> FindParcelleAsync(
        Coordinates location, CancellationToken cancellationToken)
    {
        var url = BuildWfsUrl(ParcelleLayer, "geom", location, "section,numero", count: 1);
        LogWfsRequest(ParcelleLayer, _httpClient.BaseAddress, url);
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            LogWfsResponse(response.StatusCode, ParcelleLayer, Truncate(body));

            if (!response.IsSuccessStatusCode)
            {
                LogUpstreamStatus(response.StatusCode, ParcelleLayer);
                return (null, null);
            }

            var fc = JsonSerializer.Deserialize<WfsFeatureCollection<ParcelleProperties>>(body);
            var props = fc?.Features?.FirstOrDefault()?.Properties;
            LogParcelleResult(props?.Section, props?.Numero);
            return (props?.Section, props?.Numero);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            LogRequestFailed(ParcelleLayer, ex);
            return (null, null);
        }
    }

    public async Task<bool> IsInForestAsync(Coordinates location, CancellationToken cancellationToken)
    {
        // MASQUE-FORET layer is stored in Lambert 93 (EPSG:2154) — SRSNAME is ignored by this WFS
        var (x, y) = Lambert93Converter.FromWgs84(location.Longitude, location.Latitude);
        var url = BuildWfsUrlLambert93(ForetLayer, "geom", x, y, count: 1);
        LogWfsRequest(ForetLayer, _httpClient.BaseAddress, url);
        try
        {
            using var response = await _httpClient.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            LogWfsResponse(response.StatusCode, ForetLayer, Truncate(body));

            if (!response.IsSuccessStatusCode)
            {
                LogUpstreamStatus(response.StatusCode, ForetLayer);
                return false;
            }

            var fc = JsonSerializer.Deserialize<WfsCountResult>(body);
            var isInForest = (fc?.NumberReturned ?? 0) > 0;
            LogForestResult(isInForest);
            return isInForest;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            LogRequestFailed(ForetLayer, ex);
            return false;
        }
    }

    private static string BuildWfsUrl(
        string layer, string geomField, Coordinates location, string? propertyName, int count)
    {
        var lon = location.Longitude.ToString("0.######", CultureInfo.InvariantCulture);
        var lat = location.Latitude.ToString("0.######", CultureInfo.InvariantCulture);
        var cql = $"INTERSECTS({geomField},POINT({lon} {lat}))";

        var url = "?SERVICE=WFS&VERSION=2.0.0&REQUEST=GetFeature" +
                  $"&TYPENAMES={Uri.EscapeDataString(layer)}" +
                  "&OUTPUTFORMAT=application%2Fjson" +
                  "&SRSNAME=CRS%3A84" +
                  $"&CQL_FILTER={Uri.EscapeDataString(cql)}" +
                  $"&COUNT={count}";

        if (propertyName is not null)
            url += $"&PROPERTYNAME={Uri.EscapeDataString(propertyName)}";

        return url;
    }

    private static string BuildWfsUrlLambert93(string layer, string geomField, double x, double y, int count)
    {
        var xStr = x.ToString("0.###", CultureInfo.InvariantCulture);
        var yStr = y.ToString("0.###", CultureInfo.InvariantCulture);
        var cql = $"INTERSECTS({geomField},POINT({xStr} {yStr}))";

        return "?SERVICE=WFS&VERSION=2.0.0&REQUEST=GetFeature" +
               $"&TYPENAMES={Uri.EscapeDataString(layer)}" +
               "&OUTPUTFORMAT=application%2Fjson" +
               $"&CQL_FILTER={Uri.EscapeDataString(cql)}" +
               $"&COUNT={count}";
    }

    private static string Truncate(string s) => s.Length > 1000 ? s[..1000] + "…" : s;

    private sealed record WfsFeatureCollection<TProps>(
        [property: JsonPropertyName("numberReturned")] int NumberReturned,
        [property: JsonPropertyName("features")] WfsFeature<TProps>[]? Features);

    private sealed record WfsFeature<TProps>(
        [property: JsonPropertyName("properties")] TProps? Properties);

    private sealed record ParcelleProperties(
        [property: JsonPropertyName("section")] string? Section,
        [property: JsonPropertyName("numero")] string? Numero);

    private sealed record WfsCountResult(
        [property: JsonPropertyName("numberReturned")] int NumberReturned);
}
