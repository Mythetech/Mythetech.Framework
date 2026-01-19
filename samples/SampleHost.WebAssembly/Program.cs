using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Mcp;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Secrets;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.WebAssembly;
using Mythetech.Framework.WebAssembly.Environment;
using SampleHost.Shared.Settings;
using SampleHost.WebAssembly;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddWebAssemblyServices();
builder.Services.AddMessageBus();
builder.Services.AddPluginFramework();
builder.Services.AddSecretManagerFramework();
builder.Services.AddRuntimeEnvironment();
builder.Services.AddHttpClient();
builder.Services.AddSettingsFramework();
builder.Services.AddWebAssemblySettingsStorage();
builder.Services.AddPluginStateProvider();

// Register settings from assemblies (new DI-friendly API)
builder.Services.RegisterSettingsFromAssembly(typeof(SampleAppSettings).Assembly);
builder.Services.RegisterSettingsFromAssembly(typeof(Mythetech.Framework.Infrastructure.Plugins.PluginSettings).Assembly);

var host = builder.Build();

host.Services.UseMessageBus();
host.Services.UsePluginFramework();
host.Services.UseSettingsFramework();

// In WASM, we load the plugin from the referenced assembly directly
// (dynamic DLL loading is not supported in browser WASM)
await host.Services.UsePluginAsync(typeof(SamplePlugin.SamplePluginManifest).Assembly);

await host.RunAsync();

