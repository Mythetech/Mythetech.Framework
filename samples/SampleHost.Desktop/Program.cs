using Microsoft.Extensions.DependencyInjection;
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
        
        builder.Services.AddLogging();
        builder.Services.AddMudServices();
        builder.Services.AddDesktopServices();
        builder.Services.AddMessageBus();
        builder.Services.AddPluginFramework();
        
        builder.RootComponents.Add<App>("app");
        
        var app = builder.Build();
        
        app.Services.UseMessageBus();
        app.Services.UsePlugins();
        
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

