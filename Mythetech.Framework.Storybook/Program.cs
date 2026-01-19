using Mythetech.Framework.Storybook;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.WebAssembly;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient();

builder.Services.AddMudServices(config =>
{
    config.PopoverOptions.ThrowOnDuplicateProvider = false;
});

builder.Services.AddMudMarkdownServices();

builder.Services.AddWebAssemblyServices();

builder.Services.AddRuntimeEnvironment();

builder.Services.AddMessageBus();

builder.Services.AddPluginFramework();

var host = builder.Build();

host.Services.UseMessageBus();

await host.Services.UsePluginsAsync();

await host.RunAsync();