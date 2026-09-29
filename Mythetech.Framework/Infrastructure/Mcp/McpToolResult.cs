using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Result of tool execution, wrapping content for MCP response.
/// </summary>
public class McpToolResult
{
    /// <summary>
    /// Content items returned by the tool (text, images, etc.)
    /// </summary>
    public required IReadOnlyList<McpContent> Content { get; init; }

    /// <summary>
    /// Whether the tool execution represents an error.
    /// </summary>
    public bool IsError { get; init; } = false;

    /// <summary>
    /// Factory for successful text result
    /// </summary>
    public static McpToolResult Text(string text) => new()
    {
        Content = [new McpTextContent { Text = text }]
    };

    /// <summary>
    /// Factory for error result
    /// </summary>
    public static McpToolResult Error(string message) => new()
    {
        Content = [new McpTextContent { Text = message }],
        IsError = true
    };

    /// <summary>
    /// Factory for a successful result holding a value as JSON text.
    /// Strings are returned as-is rather than as a quoted JSON string.
    /// </summary>
    public static McpToolResult Json<T>(T value) =>
        Text(value is string text ? text : JsonSerializer.Serialize<object?>(value, JsonOptions));

    /// <summary>
    /// Factory for a result from a <see cref="ToolResult{T}"/>: its value as JSON text on success,
    /// or an error result with the failure's code and message.
    /// </summary>
    public static McpToolResult FromToolResult<T>(ToolResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Error is { } error ? Error(error.ToString()) : Json(result.Value);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}

/// <summary>
/// Base class for MCP content items
/// </summary>
public abstract class McpContent
{
    /// <summary>
    /// The content type identifier
    /// </summary>
    public abstract string Type { get; }
}

/// <summary>
/// Text content returned by a tool
/// </summary>
public class McpTextContent : McpContent
{
    /// <inheritdoc />
    public override string Type => "text";

    /// <summary>
    /// The text content
    /// </summary>
    public required string Text { get; init; }
}
