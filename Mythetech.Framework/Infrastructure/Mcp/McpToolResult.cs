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
