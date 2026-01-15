using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Mythetech.Framework.Desktop;
using Mythetech.Framework.Desktop.Environment;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Mcp;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Secrets;
using Mythetech.Framework.Infrastructure.Settings;
using Photino.Blazor;
using SampleHost.Desktop;
using SampleHost.Shared.Settings;

class Program
{
    [STAThread]
    static async Task Main(string[] args)
    {
        if (await McpRegistrationExtensions.TryRunMcpServerAsync(args, options =>
        {
            options.ServerName = "SampleHost.Desktop";
            options.ServerVersion = "1.0.0";
        }))
        {
            return;
        }

        RunDesktopApp(args);
    }

    static void RunDesktopApp(string[] args)
    {
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        builder.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });
        builder.Services.AddMudServices();
        builder.Services.AddDesktopServices();
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
        builder.Services.AddDesktopSettingsStorage("SampleHost");
        builder.Services.AddPluginStateProvider("SampleHost");

        builder.RootComponents.Add<App>("app");

        var app = builder.Build();

        app.Services.UseMessageBus();
        app.Services.UseSecretManager();
        app.Services.UseMcp();
        app.Services.UsePluginFramework();
        app.Services.UseSettingsFramework();
        app.Services.RegisterSettings<SampleAppSettings>();
        app.Services.RegisterSettings<PluginSettings>();
        app.Services.RegisterSettings<McpSettings>();

        // Plugin loading is deferred to MainLayout.OnAfterRenderAsync
        // This allows custom plugin directory setting to take effect

        app.MainWindow
            .SetTitle("Sample Host (Desktop)")
            .SetSize(1920, 1080)
            .SetLogVerbosity(0)
            .SetUseOsDefaultSize(false);

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            Console.Error.WriteLine($"Unhandled exception: {args.ExceptionObject}");
        };

        app.Run();
    }
}
