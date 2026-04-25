using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;
using Serilog;
using VeilleBoisee.Api.Middleware;
using VeilleBoisee.Api.RateLimiting;
using VeilleBoisee.Application;
using VeilleBoisee.Infrastructure;
using VeilleBoisee.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter()));

JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
        options.Audience = builder.Configuration["Auth0:Audience"];
    });

builder.Services.AddAuthorization(options =>
    options.AddPolicy("IsCitizen", policy => policy
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)));

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddHttpContextAccessor()
    .AddScoped<VeilleBoisee.Api.Auth.CollectiviteContext>();

builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new()
        {
            Title = "Veille Boisée API",
            Version = "v1",
            Description = "API de signalement de décharges sauvages en forêt."
        };
        return Task.CompletedTask;
    });
});

const string FrontendCorsPolicy = "frontend";
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "PATCH")
              .WithHeaders("Content-Type", "Authorization");
    }));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<VeilleBoiseeDbContext>().Database;
    if (app.Environment.IsDevelopment())
        await database.EnsureDeletedAsync();
    await database.EnsureCreatedAsync();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Veille Boisée — API";
        options.Theme = ScalarTheme.DeepSpace;
    });
}
else
{
    app.UseHsts();
}

app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();

// Placé après UseAuthentication pour avoir accès à l'userId, avant UseAuthorization
// pour compter aussi les requêtes non-authentifiées sur les endpoints protégés.
// Désactivé en environnement de test pour ne pas perturber les tests d'intégration.
if (!app.Environment.IsEnvironment("Test"))
    app.UseRateLimiter();

app.UseAuthorization();
app.MapControllers();

app.Run();
