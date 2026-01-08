using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mythetech.Framework.Infrastructure.Mcp.Protocol.Messages;

/// <summary>
/// Parameters for the tools/call request
/// </summary>
public class McpToolCallParams
{
    /// <summary>
    /// Name of the tool to invoke
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Arguments to pass to the tool
    /// </summary>
    [JsonPropertyName("arguments")]
    public JsonElement? Arguments { get; init; }
}

/// <summary>
/// Result of the tools/call request
/// </summary>
public class McpToolCallResult
{
    /// <summary>
    /// Content returned by the tool
    /// </summary>
    [JsonPropertyName("content")]
    public required IReadOnlyList<McpContentItem> Content { get; init; }

    /// <summary>
    /// Whether the result represents an error
    /// </summary>
    [JsonPropertyName("isError")]
    public bool? IsError { get; init; }
}

/// <summary>
/// Content item in a tool call result
/// </summary>
public class McpContentItem
{
    /// <summary>
    /// Content type (e.g., "text")
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Text content (for type "text")
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}
