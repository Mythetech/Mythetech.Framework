using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Mcp.Server;

namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Central state for managing the MCP server lifecycle.
/// Registered as a Singleton in DI.
/// </summary>
public class McpServerState : IDisposable
{
    private readonly IMcpServer _server;
    private readonly McpToolRegistry _registry;
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
        ILogger<McpServerState> logger)
    {
        _server = server;
        _registry = registry;
        _logger = logger;
    }

    /// <summary>
    /// Whether the MCP server is currently running.
    /// </summary>
    public bool IsRunning => _serverTask is not null && !_serverTask.IsCompleted;

    /// <summary>
    /// All registered tools.
    /// </summary>
    public IReadOnlyList<McpToolDescriptor> RegisteredTools => _registry.GetAllTools();

    /// <summary>
    /// Start the MCP server in the background.
    /// </summary>
    public Task StartAsync()
    {
        if (IsRunning)
        {
            _logger.LogWarning("MCP server is already running");
            return Task.CompletedTask;
        }

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
        return Task.CompletedTask;
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
