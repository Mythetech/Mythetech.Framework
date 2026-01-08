using System.Text.Json.Serialization;

namespace Mythetech.Framework.Infrastructure.Mcp.Protocol.JsonRpc;

/// <summary>
/// JSON-RPC 2.0 error object
/// </summary>
public class JsonRpcError
{
    /// <summary>
    /// Error code
    /// </summary>
    [JsonPropertyName("code")]
    public required int Code { get; init; }

    /// <summary>
    /// Error message
    /// </summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>
    /// Optional additional error data
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; init; }

    // Standard JSON-RPC error codes

    /// <summary>Parse error - Invalid JSON was received</summary>
    public const int ParseError = -32700;

    /// <summary>Invalid Request - The JSON sent is not a valid Request object</summary>
    public const int InvalidRequest = -32600;

    /// <summary>Method not found - The method does not exist</summary>
    public const int MethodNotFound = -32601;

    /// <summary>Invalid params - Invalid method parameters</summary>
    public const int InvalidParams = -32602;

    /// <summary>Internal error - Internal JSON-RPC error</summary>
    public const int InternalError = -32603;
}
