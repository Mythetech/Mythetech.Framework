using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Mcp.Consumers;
using Mythetech.Framework.Infrastructure.Mcp.Messages;
using Mythetech.Framework.Infrastructure.Mcp.Server;
using Mythetech.Framework.Infrastructure.Mcp.Tools;
using Mythetech.Framework.Infrastructure.Mcp.Transport;
using Mythetech.Framework.Infrastructure.Settings.Events;

namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Extensions for registering MCP framework services.
/// </summary>
public static class McpRegistrationExtensions
{
    /// <summary>
    /// Add MCP framework services to the DI container.
    /// </summary>
    public static IServiceCollection AddMcp(this IServiceCollection services)
        => services.AddMcp(_ => { });

    /// <summary>
    /// Add MCP framework services with configuration.
    /// </summary>
    public static IServiceCollection AddMcp(this IServiceCollection services, Action<McpServerOptions> configure)
    {
        services.Configure(configure);

        // Core services
        services.AddSingleton<McpToolRegistry>();
        services.AddSingleton<McpToolLoader>();

        // Handler for MessageBus integration
        services.AddTransient<McpToolCallHandler>();

        // Transport (stdio by default, can be overridden before calling AddMcp)
        // Note: StdioMcpTransport is not supported on browser platform
        #pragma warning disable CA1416
        services.TryAddSingleton<IMcpTransport, StdioMcpTransport>();
        #pragma warning restore CA1416

        // Server
        services.AddSingleton<IMcpServer, McpServer>();
        services.AddSingleton<McpServerState>();

        // Message consumers for enable/disable
        services.AddTransient<EnableMcpServerConsumer>();
        services.AddTransient<DisableMcpServerConsumer>();

        // Settings consumer
        services.AddTransient<McpSettingsConsumer>();

        // Built-in tools (automatically included)
        services.AddTransient<GetAppInfoTool>();

        return services;
    }

    /// <summary>
    /// Register MCP tools from the calling assembly.
    /// </summary>
    public static IServiceCollection AddMcpTools(this IServiceCollection services)
        => services.AddMcpTools(Assembly.GetCallingAssembly());

    /// <summary>
    /// Register MCP tools from the specified assembly.
    /// </summary>
    public static IServiceCollection AddMcpTools(this IServiceCollection services, Assembly assembly)
    {
        var toolTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => typeof(IMcpTool).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttribute<McpToolAttribute>() is not null);

        foreach (var toolType in toolTypes)
        {
            services.AddTransient(toolType);
        }

        return services;
    }

    /// <summary>
    /// Register a specific tool type.
    /// </summary>
    public static IServiceCollection AddMcpTool<TTool>(this IServiceCollection services)
        where TTool : class, IMcpTool
    {
        services.AddTransient<TTool>();
        return services;
    }

    /// <summary>
    /// Register HTTP transport for MCP instead of the default stdio transport.
    /// This enables MCP over HTTP POST/SSE per the Streamable HTTP transport spec.
    /// Call this BEFORE calling AddMcp() to override the default transport.
    /// Note: HTTP transport is not supported on browser platform.
    /// </summary>
    [UnsupportedOSPlatform("browser")]
    public static IServiceCollection AddMcpHttpTransport(this IServiceCollection services)
    {
        // Remove any existing transport registration
        var existingTransport = services.FirstOrDefault(d => d.ServiceType == typeof(IMcpTransport));
        if (existingTransport != null)
        {
            services.Remove(existingTransport);
        }

        services.AddSingleton<IMcpTransport>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;
            var logger = sp.GetService<ILogger<HttpMcpTransport>>();
            return new HttpMcpTransport(options, logger);
        });

        return services;
    }

    /// <summary>
    /// Start the MCP HTTP server and return the endpoint URL.
    /// The server runs alongside the main application.
    /// </summary>
    /// <returns>The HTTP endpoint URL (e.g., http://localhost:3333/mcp)</returns>
    [UnsupportedOSPlatform("browser")]
    public static async Task<string?> StartMcpHttpServerAsync(this IServiceProvider services)
    {
        var state = services.GetRequiredService<McpServerState>();
        await state.StartAsync();
        return state.HttpEndpoint;
    }

    /// <summary>
    /// Initialize MCP services and register tools to the registry.
    /// Call after building the service provider.
    /// </summary>
    public static IServiceProvider UseMcp(this IServiceProvider services)
        => services.UseMcp(Assembly.GetCallingAssembly());

    /// <summary>
    /// Initialize MCP services with tools from the specified assemblies.
    /// </summary>
    public static IServiceProvider UseMcp(this IServiceProvider services, params Assembly[] assemblies)
    {
        var registry = services.GetRequiredService<McpToolRegistry>();
        var loader = services.GetRequiredService<McpToolLoader>();
        var messageBus = services.GetRequiredService<IMessageBus>();

        // Register tool call handler to MessageBus
        messageBus.RegisterQueryHandler<McpToolCallMessage, McpToolCallResponse, McpToolCallHandler>();

        // Register enable/disable consumers
        messageBus.RegisterConsumerType<EnableMcpServerMessage, EnableMcpServerConsumer>();
        messageBus.RegisterConsumerType<DisableMcpServerMessage, DisableMcpServerConsumer>();

        // Register settings consumer
        messageBus.RegisterConsumerType<SettingsModelChanged<McpSettings>, McpSettingsConsumer>();

        // Register built-in tools from the framework
        foreach (var descriptor in loader.DiscoverTools(typeof(GetAppInfoTool).Assembly))
        {
            registry.RegisterTool(descriptor);
        }

        // Discover and register tools from specified assemblies
        foreach (var assembly in assemblies)
        {
            foreach (var descriptor in loader.DiscoverTools(assembly))
            {
                registry.RegisterTool(descriptor);
            }
        }

        return services;
    }

    /// <summary>
    /// Checks if the application was started with --mcp flag and runs in MCP server mode if so.
    /// Call this at the start of Main() before building your normal app.
    /// Returns true if MCP mode was handled (app should exit), false to continue normal startup.
    /// </summary>
    /// <param name="args">Command line arguments</param>
    /// <param name="configure">Optional configuration for MCP server options</param>
    /// <returns>True if MCP server ran and completed, false if normal app should start</returns>
    public static Task<bool> TryRunMcpServerAsync(string[] args, Action<McpServerOptions>? configure = null)
    {
        // Note: Assembly.GetCallingAssembly() must be captured in a non-async method
        // because async state machines can break the call stack inspection
        var callingAssembly = Assembly.GetCallingAssembly();
        return TryRunMcpServerAsync(args, configure, callingAssembly);
    }

    /// <summary>
    /// Checks if the application was started with --mcp flag and runs in MCP server mode if so.
    /// </summary>
    public static async Task<bool> TryRunMcpServerAsync(string[] args, Action<McpServerOptions>? configure, params Assembly[] toolAssemblies)
    {
        if (!args.Contains("--mcp"))
            return false;

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddMessageBus();
        services.AddMcp(configure ?? (_ => { }));

        foreach (var assembly in toolAssemblies)
        {
            services.AddMcpTools(assembly);
        }

        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.UseMessageBus();
        serviceProvider.UseMcp(toolAssemblies);

        var server = serviceProvider.GetRequiredService<IMcpServer>();

        Console.Error.WriteLine("MCP server starting...");
        await server.RunAsync();
        Console.Error.WriteLine("MCP server stopped.");

        return true;
    }

    /// <summary>
    /// Checks if the application was started with --mcp flag and runs in MCP server mode if so.
    /// Allows customization of the service collection for logging, etc.
    /// </summary>
    public static async Task<bool> TryRunMcpServerAsync(
        string[] args,
        Action<McpServerOptions>? configure,
        Action<IServiceCollection>? configureServices,
        params Assembly[] toolAssemblies)
    {
        if (!args.Contains("--mcp"))
            return false;

        var services = new ServiceCollection();

        services.AddLogging();
        configureServices?.Invoke(services);

        services.AddMessageBus();
        services.AddMcp(configure ?? (_ => { }));

        foreach (var assembly in toolAssemblies)
        {
            services.AddMcpTools(assembly);
        }

        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.UseMessageBus();
        serviceProvider.UseMcp(toolAssemblies);

        var server = serviceProvider.GetRequiredService<IMcpServer>();

        Console.Error.WriteLine("MCP server starting...");
        await server.RunAsync();
        Console.Error.WriteLine("MCP server stopped.");

        return true;
    }
}
