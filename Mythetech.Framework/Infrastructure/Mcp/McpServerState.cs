using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Mcp.Server;
using Mythetech.Framework.Infrastructure.Mcp.Transport;

namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Central state for managing the MCP server lifecycle.
/// Registered as a Singleton in DI.
/// </summary>
public class McpServerState : IDisposable
{
    private readonly IMcpServer _server;
    private readonly McpToolRegistry _registry;
    private readonly IMcpTransport _transport;
    private readonly ILogger<McpServerState> _logger;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;
    private bool _disposed;

    /// <summary>
    /// Raised when server state changes (started, stopped, etc.)
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Creates a new instance of McpServerState.
    /// </summary>
    public McpServerState(
        IMcpServer server,
        McpToolRegistry registry,
        IMcpTransport transport,
        ILogger<McpServerState> logger)
    {
        _server = server;
        _registry = registry;
        _transport = transport;
        _logger = logger;
    }

    /// <summary>
    /// Whether the MCP server is currently running.
    /// </summary>
    public bool IsRunning => _serverTask is not null && !_serverTask.IsCompleted;

    /// <summary>
    /// All registered tools (regardless of enabled state).
    /// </summary>
    public IReadOnlyList<McpToolDescriptor> RegisteredTools => _registry.GetAllTools();

    /// <summary>
    /// Check if a tool is enabled.
    /// </summary>
    public bool IsToolEnabled(string name) => _registry.IsToolEnabled(name);

    /// <summary>
    /// Set whether a tool is enabled.
    /// </summary>
    public async Task SetToolEnabledAsync(string name, bool enabled)
    {
        await _registry.SetToolEnabledAsync(name, enabled);
        NotifyStateChanged();
    }

    /// <summary>
    /// The HTTP endpoint URL when HTTP MCP is running, or null if not available.
    /// Example: http://localhost:3333/mcp
    /// </summary>
    public string? HttpEndpoint => GetHttpEndpoint();

    private string? GetHttpEndpoint()
    {
        // HttpMcpTransport is not supported on browser platform, but this
        // property is safe to call - it will return null if transport isn't HTTP
        #pragma warning disable CA1416
        return (_transport as HttpMcpTransport)?.Endpoint;
        #pragma warning restore CA1416
    }

    /// <summary>
    /// Start the MCP server in the background.
    /// </summary>
    public async Task StartAsync()
    {
        if (IsRunning)
        {
            _logger.LogWarning("MCP server is already running");
            return;
        }

        // Start HTTP transport if applicable
        // HttpMcpTransport is not supported on browser platform, but this check
        // is safe - if transport is HttpMcpTransport, we're not on browser
        #pragma warning disable CA1416
        if (_transport is HttpMcpTransport httpTransport)
        {
            await httpTransport.StartAsync();
        }
        #pragma warning restore CA1416

        _cts = new CancellationTokenSource();
        _serverTask = Task.Run(async () =>
        {
            try
            {
                await _server.RunAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("MCP server stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP server error");
            }
            finally
            {
                NotifyStateChanged();
            }
        });

        _logger.LogInformation("MCP server started");
        NotifyStateChanged();
    }

    /// <summary>
    /// Stop the MCP server.
    /// </summary>
    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            _logger.LogWarning("MCP server is not running");
            return;
        }

        _logger.LogInformation("Stopping MCP server...");
        _cts?.Cancel();

        if (_serverTask is not null)
        {
            try
            {
                await _serverTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("MCP server did not stop gracefully within timeout");
            }
        }

        // Stop HTTP transport if applicable (allows restart)
        #pragma warning disable CA1416
        if (_transport is HttpMcpTransport httpTransport)
        {
            await httpTransport.StopAsync();
        }
        #pragma warning restore CA1416

        _cts?.Dispose();
        _cts = null;
        _serverTask = null;

        NotifyStateChanged();
    }

    /// <summary>
    /// Toggle the MCP server on or off.
    /// </summary>
    public async Task ToggleAsync()
    {
        if (IsRunning)
            await StopAsync();
        else
            await StartAsync();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
