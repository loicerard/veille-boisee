namespace VeilleBoisee.Infrastructure.Enrichment;

public sealed class ApiCartoOptions
{
    public const string SectionName = "ApiCarto";

    public Uri BaseAddress { get; set; } = new("https://apicarto.ign.fr/api/");
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(8);
}
