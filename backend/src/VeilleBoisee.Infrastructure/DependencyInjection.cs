using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using VeilleBoisee.Application.Abstractions;
using VeilleBoisee.Infrastructure.Geocoding;
using VeilleBoisee.Infrastructure.Enrichment;
using VeilleBoisee.Infrastructure.Persistence;
using VeilleBoisee.Infrastructure.Persistence.Repositories;
using VeilleBoisee.Infrastructure.Photos;
using VeilleBoisee.Infrastructure.Security;

namespace VeilleBoisee.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GeoApiGouvFrOptions>(configuration.GetSection(GeoApiGouvFrOptions.SectionName));

        services.AddHttpClient<IGeocodingService, GeoApiGouvFrClient>(GeoApiGouvFrClient.HttpClientName,
                (provider, http) =>
                {
                    var options = provider.GetRequiredService<IOptions<GeoApiGouvFrOptions>>().Value;
                    http.BaseAddress = options.BaseAddress;
                    http.Timeout = options.RequestTimeout;
                })
            .AddStandardResilienceHandler(static options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.UseJitter = true;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
            });

        var connectionString = configuration.GetConnectionString("DefaultConnection")!;
        services.AddDbContext<VeilleBoiseeDbContext>(options =>
        {
            if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(connectionString);
            else
                options.UseSqlServer(connectionString);
        });

        services.AddScoped<IReportRepository, ReportRepository>();

        services.Configure<EmailEncryptionOptions>(configuration.GetSection(EmailEncryptionOptions.SectionName));
        services.AddSingleton<IEmailEncryptionService, AesEmailEncryptionService>();

        services.Configure<GeoplatformeOptions>(configuration.GetSection(GeoplatformeOptions.SectionName));
        services.AddHttpClient<GeoplatformeWfsClient>((provider, http) =>
            {
                var options = provider.GetRequiredService<IOptions<GeoplatformeOptions>>().Value;
                http.BaseAddress = options.WfsBaseAddress;
                http.Timeout = options.RequestTimeout;
            })
            .AddStandardResilienceHandler(static options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.UseJitter = true;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(8);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(25);
            });

        services.Configure<ApiCartoOptions>(configuration.GetSection(ApiCartoOptions.SectionName));
        services.AddHttpClient<Natura2000Client>((provider, http) =>
            {
                var options = provider.GetRequiredService<IOptions<ApiCartoOptions>>().Value;
                http.BaseAddress = options.BaseAddress;
                http.Timeout = options.RequestTimeout;
            })
            .AddStandardResilienceHandler(static options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.Retry.UseJitter = true;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(6);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
            });

        services.AddTransient<IGeographicEnrichmentService, GeographicEnrichmentService>();

        services.AddSingleton<IExifStrippingService, ImageSharpExifStrippingService>();

        return services;
    }
}
