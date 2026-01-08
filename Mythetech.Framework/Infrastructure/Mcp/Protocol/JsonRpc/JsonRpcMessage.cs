using System.Text.Json.Serialization;

namespace Mythetech.Framework.Infrastructure.Mcp.Protocol.JsonRpc;

/// <summary>
/// Base JSON-RPC 2.0 message
/// </summary>
public abstract class JsonRpcMessage
{
    /// <summary>
    /// JSON-RPC version (always "2.0")
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";
}
