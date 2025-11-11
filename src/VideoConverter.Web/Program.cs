using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MudBlazor.Services;

using VideoConverter.Web;
using VideoConverter.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API base address
// In development, API runs on a different port; in production, same origin
var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ??
    (builder.HostEnvironment.IsDevelopment()
        ? "http://localhost:5124"
        : builder.HostEnvironment.BaseAddress);

_ = builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseAddress) });
_ = builder.Services.AddScoped<IVideoConverterApiClient, VideoConverterApiClient>();
builder.Services.AddMudServices();

await builder.Build().RunAsync().ConfigureAwait(false);
