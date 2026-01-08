using Mythetech.Framework.Infrastructure.Mcp.Protocol.JsonRpc;

namespace Mythetech.Framework.Infrastructure.Mcp.Transport;

/// <summary>
/// Abstraction for MCP message transport (stdio, HTTP SSE, etc.)
/// </summary>
public interface IMcpTransport : IAsyncDisposable
{
    /// <summary>
    /// Read the next JSON-RPC message from the transport.
    /// Returns null on end of stream.
    /// </summary>
    Task<JsonRpcRequest?> ReadMessageAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Write a JSON-RPC response to the transport.
    /// </summary>
    Task WriteMessageAsync(JsonRpcResponse response, CancellationToken cancellationToken = default);

    /// <summary>
    /// Write a JSON-RPC notification to the transport.
    /// </summary>
    Task WriteNotificationAsync(string method, object? @params, CancellationToken cancellationToken = default);
}
