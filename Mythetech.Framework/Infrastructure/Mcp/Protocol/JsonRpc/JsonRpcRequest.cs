using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mythetech.Framework.Infrastructure.Mcp.Protocol.JsonRpc;

/// <summary>
/// JSON-RPC 2.0 request message
/// </summary>
public class JsonRpcRequest : JsonRpcMessage
{
    /// <summary>
    /// Request identifier. Null for notifications.
    /// </summary>
    [JsonPropertyName("id")]
    public object? Id { get; init; }

    /// <summary>
    /// The method to invoke
    /// </summary>
    [JsonPropertyName("method")]
    public required string Method { get; init; }

    /// <summary>
    /// Method parameters as raw JSON
    /// </summary>
    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }

    /// <summary>
    /// True if this is a notification (no id = no response expected)
    /// </summary>
    [JsonIgnore]
    public bool IsNotification => Id is null;
}
