using Mythetech.Components.Storybook;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Components.Infrastructure.MessageBus;
using Mythetech.Components.WebAssembly;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMemoryCache();

builder.Services.AddMudServices(config =>
{
    config.PopoverOptions.ThrowOnDuplicateProvider = false;
});

builder.Services.AddMudMarkdownServices();


builder.Services.AddLinkOpeningService();

builder.Services.AddMessageBus();

var host = builder.Build();

host.Services.UseMessageBus();
    
await host.RunAsync();