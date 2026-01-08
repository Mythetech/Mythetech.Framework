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
}
