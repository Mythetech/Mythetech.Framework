using Hermes;
using Hermes.Blazor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Mythetech.Framework.Desktop;
using Mythetech.Framework.Desktop.Hermes;
using Mythetech.Framework.Desktop.Storage.LiteDb;
using Mythetech.Framework.Desktop.Environment;
using Mythetech.Framework.Infrastructure.FeatureFlags;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Mcp;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Secrets;
using Mythetech.Framework.Infrastructure.Settings;
using SampleHost.Desktop;
using SampleHost.Shared.Settings;

class Program
{
    // Hermes requires a synchronous STA entry point on Windows; async work is joined explicitly.
    [STAThread]
    static void Main(string[] args)
    {
        var ranAsMcpServer = McpRegistrationExtensions.TryRunMcpServerAsync(args, options =>
        {
            options.ServerName = "SampleHost.Desktop";
            options.ServerVersion = "1.0.0";
        }).GetAwaiter().GetResult();

        if (ranAsMcpServer)
        {
            return;
        }

        RunDesktopApp(args);
    }

    static void RunDesktopApp(string[] args)
    {
        HermesWindow.Prewarm();

        var builder = HermesBlazorAppBuilder.CreateDefault(args);
        builder.ConfigureWindow(options =>
        {
            options.Title = "Sample Host (Desktop)";
            options.Width = 1920;
            options.Height = 1080;
            options.CenterOnScreen = true;
            options.DevToolsEnabled = true;
        });

        builder.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });
        builder.Services.AddMudServices();
        builder.Services.AddDesktopServices(DesktopHost.Hermes);
        builder.Services.AddMessageBus();
        builder.Services.AddPluginFramework();
        builder.Services.AddOnePasswordSecretManager();
        builder.Services.AddNativeSecretManager();
        builder.Services.AddHttpClient();
        builder.Services.AddRuntimeEnvironment(DesktopRuntimeEnvironment.Development());
        builder.Services.AddMcp(options =>
        {
            options.ServerName = "SampleHost.Desktop";
            options.ServerVersion = "1.0.0";
        });
        builder.Services.AddMcpTools();
        builder.Services.AddSettingsFramework();
        builder.Services.AddFeatureFlags();
        builder.Services.AddDesktopSettingsStorage("SampleHost");
        builder.Services.AddPluginStateProvider("SampleHost");

        // Register settings from assemblies (new DI-friendly API)
        builder.Services.RegisterSettingsFromAssembly(typeof(SampleAppSettings).Assembly);
        builder.Services.RegisterSettingsFromAssembly(typeof(PluginSettings).Assembly);

        builder.RootComponents.Add<App>("app");

        var app = builder.Build();
        app.RegisterHermesProvider();

        app.Services.UseMessageBus();
        app.Services.UseSecretManager();
        app.Services.UseMcp();
        app.Services.UsePluginFramework();
        app.Services.UseSettingsFramework();
        app.Services.LoadPersistedSettingsAsync().GetAwaiter().GetResult();
        app.Services.UseFeatureFlags().GetAwaiter().GetResult();

        // Plugin loading is deferred to MainLayout.OnAfterRenderAsync
        // This allows custom plugin directory setting to take effect

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            Console.Error.WriteLine($"Unhandled exception: {args.ExceptionObject}");
        };

        app.Run();
        app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
