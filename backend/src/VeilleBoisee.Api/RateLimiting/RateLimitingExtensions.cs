using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace VeilleBoisee.Api.RateLimiting;

internal static class RateLimitingExtensions
{
    internal static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var publicLimit = configuration.GetValue("RateLimiting:PublicPermitLimit", 30);
        var authenticatedLimit = configuration.GetValue("RateLimiting:AuthenticatedPermitLimit", 100);
        var submitReportLimit = configuration.GetValue("RateLimiting:SubmitReportPermitLimit", 5);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // 30 req/min par IP pour les endpoints publics
            options.AddPolicy(RateLimitingPolicies.Public, httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = publicLimit,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6
                    }));

            // 100 req/min par userId pour les endpoints authentifiés (repli sur IP si anonyme)
            options.AddPolicy(RateLimitingPolicies.Authenticated, httpContext =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;
                var partitionKey = userId ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey,
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = userId is not null ? authenticatedLimit : publicLimit,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6
                    });
            });

            // 5 req/heure par userId pour la soumission de signalement
            options.AddPolicy(RateLimitingPolicies.SubmitReport, httpContext =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;
                var partitionKey = userId ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = submitReportLimit,
                        Window = TimeSpan.FromHours(1)
                    });
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                var response = context.HttpContext.Response;
                response.ContentType = "application/json";

                var retryAfterSeconds = 60;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));

                response.Headers["Retry-After"] = retryAfterSeconds.ToString();
                response.Headers["X-RateLimit-Remaining"] = "0";
                response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow
                    .AddSeconds(retryAfterSeconds)
                    .ToUnixTimeSeconds()
                    .ToString();

                await response.WriteAsJsonAsync(
                    new { retryAfter = retryAfterSeconds },
                    cancellationToken);
            };
        });

        return services;
    }
}
