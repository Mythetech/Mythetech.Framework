namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Runtime metadata about a registered tool.
/// </summary>
public class McpToolDescriptor
{
    /// <summary>
    /// Tool name (from McpToolAttribute)
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Tool description (from McpToolAttribute)
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The implementation type
    /// </summary>
    public required Type ToolType { get; init; }

    /// <summary>
    /// The input parameter type (null for tools with no input)
    /// </summary>
    public Type? InputType { get; init; }

    /// <summary>
    /// JSON Schema for the input parameters (generated from InputType)
    /// </summary>
    public object? InputSchema { get; init; }
}
