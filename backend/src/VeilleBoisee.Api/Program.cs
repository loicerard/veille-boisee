using Scalar.AspNetCore;
using Serilog;
using VeilleBoisee.Api.Middleware;
using VeilleBoisee.Application;
using VeilleBoisee.Infrastructure;
using VeilleBoisee.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter()));

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddScoped<VeilleBoisee.Api.Auth.CollectiviteContext>();

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
              .WithMethods("GET", "POST")
              .WithHeaders("Content-Type");
    }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<VeilleBoiseeDbContext>().Database;
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
app.MapControllers();

app.Run();
