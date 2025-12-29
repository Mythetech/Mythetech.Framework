using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.WebAssembly;
using SampleHost.WebAssembly;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMudServices();
builder.Services.AddWebAssemblyServices();
builder.Services.AddMessageBus();
builder.Services.AddPluginFramework();

var host = builder.Build();

host.Services.UseMessageBus();

// In WASM, we load the plugin from the referenced assembly directly
// (dynamic DLL loading is not supported in browser WASM)
host.Services.UsePlugin(typeof(SamplePlugin.SamplePluginManifest).Assembly);

await host.RunAsync();

