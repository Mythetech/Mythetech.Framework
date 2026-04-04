namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Marks a record/class as a fire-and-forget command to be exposed as an MCP tool.
/// The command will be published via IMessageBus.PublishAsync without awaiting a response.
/// </summary>
/// <remarks>
/// Tool metadata is inferred from the type:
/// <list type="bullet">
///   <item>Name: Derived from type name (PascalCase to snake_case), or override with <see cref="Name"/></item>
///   <item>Description: Derived from XML summary, or override with <see cref="Description"/></item>
///   <item>Parameters: Derived from primary constructor parameters</item>
///   <item>Parameter descriptions: Derived from XML param docs</item>
///   <item>Required vs optional: Derived from nullability and default values</item>
/// </list>
/// Requires the Mythetech.Framework.AI.Generator package to generate the MCP tool implementation.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ToolCommandAttribute : Attribute
{
    /// <summary>
    /// Optional override for the tool name. If not specified, derived from the type name
    /// using snake_case conversion (e.g., BuildSolution becomes build_solution).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional override for the tool description. If not specified, derived from the
    /// type's XML summary documentation.
    /// </summary>
    public string? Description { get; set; }
}
