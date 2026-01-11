namespace Mythetech.Framework.Infrastructure.Mcp.Server;

/// <summary>
/// MCP server abstraction for handling protocol messages.
/// </summary>
public interface IMcpServer
{
    /// <summary>
    /// Run the server until the transport closes or cancellation is requested.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a notification to connected clients that the tools list has changed.
    /// </summary>
    Task NotifyToolsListChangedAsync(CancellationToken cancellationToken = default);
}
