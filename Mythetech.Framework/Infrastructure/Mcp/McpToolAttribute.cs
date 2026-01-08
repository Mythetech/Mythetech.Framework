namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Declares a class as an MCP tool with metadata for tool listing.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class McpToolAttribute : Attribute
{
    /// <summary>
    /// The unique tool name (used in tools/call).
    /// Convention: snake_case (e.g., "list_databases", "execute_query")
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description for the tool.
    /// Used by AI agents to understand when/how to use the tool.
    /// </summary>
    public required string Description { get; init; }
}
