using MudBlazor.Services;
using Mythetech.Framework.Infrastructure;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Secrets;
using Mythetech.Framework.Infrastructure.Settings;
using SampleHost.Server;
using SampleHost.Server.Services;
using SampleHost.Shared.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddHttpClient();
builder.Services.AddMessageBus();
builder.Services.AddPluginFramework();
// AddMessageBus registers the Framework's own consumers, and the one that restores the saved
// secret manager cannot be built without the secret manager's state.
builder.Services.AddSecretManagerFramework();
builder.Services.AddSettingsFramework();
builder.Services.AddTransient<ICopyToClipboard, ServerCopyToClipboardService>();

builder.Services.RegisterSettingsFromAssembly(typeof(SampleAppSettings).Assembly);
builder.Services.RegisterSettingsFromAssembly(typeof(PluginSettings).Assembly);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAntiforgery();
app.MapStaticAssets();

app.Services.UseMessageBus();
app.Services.UsePluginFramework();
app.Services.UseSettingsFramework();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(SamplePlugin.SamplePluginManifest).Assembly);

app.Run();
