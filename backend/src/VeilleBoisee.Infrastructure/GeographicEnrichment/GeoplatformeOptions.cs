namespace VeilleBoisee.Infrastructure.Enrichment;

public sealed class GeoplatformeOptions
{
    public const string SectionName = "Geoplateforme";

    public Uri WfsBaseAddress { get; set; } = new("https://data.geopf.fr/wfs");
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
