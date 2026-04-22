using Microsoft.Extensions.Logging;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Domain.ValueObjects;

namespace VeilleBoisee.Infrastructure.Enrichment;

internal sealed partial class GeographicEnrichmentService : IGeographicEnrichmentService
{
    private readonly GeoplatformeWfsClient _wfs;
    private readonly Natura2000Client _natura2000;
    private readonly ILogger<GeographicEnrichmentService> _logger;

    [LoggerMessage(Level = LogLevel.Information, Message = "Geographic enrichment complete for {Location}: parcelle={Section}/{Numero} forest={IsInForest} natura2000={IsInNatura2000Zone}")]
    private partial void LogEnrichmentComplete(string location, string? section, string? numero, bool isInForest, bool isInNatura2000Zone);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Geographic enrichment failed unexpectedly for {Location}.")]
    private partial void LogEnrichmentFailed(string location, Exception exception);

    public GeographicEnrichmentService(
        GeoplatformeWfsClient wfs,
        Natura2000Client natura2000,
        ILogger<GeographicEnrichmentService> logger)
    {
        _wfs = wfs;
        _natura2000 = natura2000;
        _logger = logger;
    }

    public async Task<GeographicEnrichment> EnrichAsync(Coordinates location, CancellationToken cancellationToken)
    {
        try
        {
            var parcelleTask = _wfs.FindParcelleAsync(location, cancellationToken);
            var forestTask = _wfs.IsInForestAsync(location, cancellationToken);
            var naturaTask = _natura2000.IsInNatura2000ZoneAsync(location, cancellationToken);

            await Task.WhenAll(parcelleTask, forestTask, naturaTask);

            var (section, numero) = await parcelleTask;
            var isInForest = await forestTask;
            var isInNatura2000Zone = await naturaTask;
            var coords = $"{location.Latitude},{location.Longitude}";
            LogEnrichmentComplete(coords, section, numero, isInForest, isInNatura2000Zone);
            return new GeographicEnrichment(section, numero, isInForest, isInNatura2000Zone);
        }
        catch (Exception ex)
        {
            LogEnrichmentFailed($"{location.Latitude},{location.Longitude}", ex);
            return GeographicEnrichment.Unavailable;
        }
    }
}
