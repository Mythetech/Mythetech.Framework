using System.Text.Json.Serialization;

namespace Mythetech.Framework.Infrastructure.Mcp.Protocol.Messages;

/// <summary>
/// Result of the tools/list request
/// </summary>
public class McpToolsListResult
{
    /// <summary>
    /// List of available tools
    /// </summary>
    [JsonPropertyName("tools")]
    public required IReadOnlyList<McpToolDefinition> Tools { get; init; }
}

/// <summary>
/// Definition of a tool for the tools/list response
/// </summary>
public class McpToolDefinition
{
    /// <summary>
    /// Tool name (used for tools/call)
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// JSON Schema for the input parameters
    /// </summary>
    [JsonPropertyName("inputSchema")]
    public object? InputSchema { get; init; }
}
