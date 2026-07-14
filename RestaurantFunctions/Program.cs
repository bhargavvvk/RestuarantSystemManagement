using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RestaurantAPI.Contexts;
using RestaurantAPI.Repositories;
using RestaurantAPI.RepositoryInterfaces;
using RestaurantAPI.Services;
using RestaurantAPI.ServiceInterfaces;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
var keyVaultUri = builder.Configuration["KeyVaultUri"];

if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}
builder.Services.AddDbContext<RestaurantContext>(options =>
    options.UseNpgsql(builder.Configuration["ConnectionStrings:Default"]));

builder.Services.AddDbContext<ArchiveContext>(options =>
    options.UseNpgsql(builder.Configuration["ConnectionStrings:Archive"]));
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

builder.Services.AddScoped<IArchiveAuditLogRepository, ArchiveAuditLogRepository>();
builder.Services.AddScoped<IAuditArchiveService, AuditArchiveService>();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
