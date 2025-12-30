using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Mythetech.Framework.Desktop;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Photino.Blazor;
using SampleHost.Desktop;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);
        
        // Add console logging for debugging
        builder.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Debug);
        });
        builder.Services.AddMudServices();
        builder.Services.AddDesktopServices();
        builder.Services.AddMessageBus();
        builder.Services.AddPluginFramework();
        builder.Services.AddHttpClient();
        
        builder.RootComponents.Add<App>("app");
        
        var app = builder.Build();
        
        app.Services.UseMessageBus();
        
        // Explicitly specify plugin directory and log it
        var pluginDir = Path.Combine(AppContext.BaseDirectory, "plugins");

        if (Directory.Exists(pluginDir))
        {
            // Show root DLLs
            var rootDlls = Directory.GetFiles(pluginDir, "*.dll");
            Console.WriteLine($"Root DLLs: {rootDlls.Length}");
            foreach (var dll in rootDlls)
            {
                Console.WriteLine($"  - {Path.GetFileName(dll)}");
            }
            
            // Show subdirectories (plugin folders)
            var subdirs = Directory.GetDirectories(pluginDir);
            Console.WriteLine($"Plugin folders: {subdirs.Length}");
            foreach (var subdir in subdirs)
            {
                var subDlls = Directory.GetFiles(subdir, "*.dll");
                Console.WriteLine($"  [{Path.GetFileName(subdir)}] - {subDlls.Length} DLLs");
            }
        }
        
        app.Services.UsePlugins(pluginDir);
        
        app.MainWindow
            .SetTitle("Sample Plugin Host (Desktop)")
            .SetSize(1920, 1080)
            .SetUseOsDefaultSize(false);
        
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            Console.Error.WriteLine($"Unhandled exception: {args.ExceptionObject}");
        };
        
        app.Run();
    }
}

