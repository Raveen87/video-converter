using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MudBlazor.Services;

using Serilog;

using VideoConverter.Web;
using VideoConverter.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Configure Serilog
var logLevel = builder.HostEnvironment.IsDevelopment()
    ? Serilog.Events.LogEventLevel.Debug
    : Serilog.Events.LogEventLevel.Information;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(logLevel)
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.BrowserConsole()
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API base address
// In development, API runs on a different port; in production, same origin
var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ??
    (builder.HostEnvironment.IsDevelopment()
        ? "http://localhost:5124"
        : builder.HostEnvironment.BaseAddress);

_ = builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseAddress) });
_ = builder.Services.AddScoped<IVideoConverterApiClient, VideoConverterApiClient>();
builder.Services.AddMudServices();

var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation(
    "Video Converter Web application starting. Environment: {Environment}, API Base: {ApiBase}",
    builder.HostEnvironment.Environment,
    apiBaseAddress);

await host.RunAsync().ConfigureAwait(false);
