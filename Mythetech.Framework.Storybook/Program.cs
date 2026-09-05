using Mythetech.Framework.Storybook;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;
using MudBlazor.Utilities;
using Mythetech.Framework.Infrastructure.Keyboard;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Settings;
using Mythetech.Framework.Components.CommandPalette;
using Mythetech.Framework.Infrastructure.Guards;
using Mythetech.Framework.Storybook.Stories;
using Mythetech.Framework.WebAssembly;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddHttpClient();

builder.Services.AddMudServices(config =>
{
    config.PopoverOptions.ThrowOnDuplicateProvider = false;
    config.PopoverOptions.OverflowBehavior = OverflowBehavior.FlipNever;
});

builder.Services.AddMudMarkdownServices();

builder.Services.AddWebAssemblyServices();

builder.Services.AddRuntimeEnvironment();

builder.Services.AddMessageBus();

builder.Services.AddPluginFramework();

builder.Services.AddSingleton<IJsGuardService, AlwaysReadyJsGuardService>();

builder.Services.AddCommandPalette();
builder.Services.AddCommandProvider<SampleCommandProvider>();

builder.Services.AddSingleton<ISettingsProvider, SettingsProvider>();

builder.Services.AddKeyBindings(b => b
    .Category("Requests")
        .Add("story.run", "Run request", KeyBinding.CtrlOrCmd(JsKey.Enter),
            (bus, ct) => Task.CompletedTask,
            "Sends the active request")
        .Add("story.duplicate", "Duplicate request", defaultBinding: null,
            (bus, ct) => Task.CompletedTask,
            "Ships with no shortcut until you assign one")
    .Category("View")
        .Add("story.palette", "Command palette", KeyBinding.CtrlOrCmd(JsKey.KeyK),
            (bus, ct) => Task.CompletedTask)
        .Add("story.zen", "Zen mode", KeyBinding.CtrlOrCmd(JsKey.Enter, shift: true),
            (bus, ct) => Task.CompletedTask));

var host = builder.Build();

host.Services.UseMessageBus();

await host.Services.UsePluginsAsync();

await host.RunAsync();