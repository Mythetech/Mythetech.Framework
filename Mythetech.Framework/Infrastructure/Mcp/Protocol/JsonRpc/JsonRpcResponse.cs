using System.Text.Json.Serialization;

namespace Mythetech.Framework.Infrastructure.Mcp.Protocol.JsonRpc;

/// <summary>
/// JSON-RPC 2.0 response message
/// </summary>
public class JsonRpcResponse : JsonRpcMessage
{
    /// <summary>
    /// Request identifier this response corresponds to
    /// </summary>
    [JsonPropertyName("id")]
    public required object? Id { get; init; }

    /// <summary>
    /// Result on success (mutually exclusive with Error)
    /// </summary>
    [JsonPropertyName("result")]
    public object? Result { get; init; }

    /// <summary>
    /// Error on failure (mutually exclusive with Result)
    /// </summary>
    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; init; }

    /// <summary>
    /// Create a successful response
    /// </summary>
    public static JsonRpcResponse Success(object? id, object? result) => new()
    {
        Id = id,
        Result = result
    };

    /// <summary>
    /// Create an error response
    /// </summary>
    public static JsonRpcResponse Failure(object? id, int code, string message, object? data = null) => new()
    {
        Id = id,
        Error = new JsonRpcError { Code = code, Message = message, Data = data }
    };
}
