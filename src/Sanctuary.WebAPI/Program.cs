using System.Globalization;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NLog.Extensions.Logging;

using Sanctuary.Core.Extensions;
using Sanctuary.Database;
using Sanctuary.WebAPI.Endpoints;
using Sanctuary.WebAPI.Options;
using Sanctuary.WebAPI.Services;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls();

// Proxy Server / Load Balancer
var forwardedHeaderSection = builder.Configuration.GetSection("ForwardedHeadersOptions");

if (forwardedHeaderSection is not null)
    builder.Services.Configure<ForwardedHeadersOptions>(forwardedHeaderSection);

// Options
builder.Services.AddOptionsWithValidateOnStart<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.Section)
    .ValidateOnStart();

builder.Services.AddOptionsWithValidateOnStart<WebAPIOptions>()
    .BindConfiguration(WebAPIOptions.Section)
    .ValidateOnStart();

// VpnApi key is optional at startup (not ValidateOnStart) - the service
// itself just logs a warning and skips the check if it is blank, rather
// than crash-looping the whole API before the key has even been obtained.
builder.Services.AddOptions<VpnApiOptions>()
    .BindConfiguration(VpnApiOptions.Section);

builder.Services.AddHttpClient<VpnDetectionService>();

// Database
builder.Services.AddDatabase(builder.Configuration);

// Logging
builder.Logging.ClearProviders();

#if DEBUG

builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddHttpLogging(logging =>
{
    // Bodies carry passwords and session IDs, so they are never logged.
    logging.LoggingFields = HttpLoggingFields.All & ~(HttpLoggingFields.RequestBody | HttpLoggingFields.ResponseBody);
});

#endif

var nlogConfigFile = builder.Environment.IsDevelopment()
    ? "NLog.Development.config"
    : "NLog.config";

builder.Logging.AddNLog(nlogConfigFile);

var app = builder.Build();

#if DEBUG

app.UseHttpLogging();

#endif

// Configure the HTTP request pipeline.

app.MapAuthEndpoints();
app.MapPortraitEndpoints();

app.Run();