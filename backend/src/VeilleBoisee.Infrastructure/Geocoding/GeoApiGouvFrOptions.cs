namespace VeilleBoisee.Infrastructure.Geocoding;

public sealed class GeoApiGouvFrOptions
{
    public const string SectionName = "GeoApiGouvFr";

    public Uri BaseAddress { get; set; } = new("https://geo.api.gouv.fr/");

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
