namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Marks a record/class as a request/response message to be exposed as an MCP tool.
/// The request will be sent via IMessageBus.SendAsync and the response returned to the caller.
/// Use it for any tool that returns a result, whether it reads or changes state.
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
/// The response is returned to the MCP client as JSON text, or as-is when it is a string.
/// When <see cref="ResponseType"/> is a <see cref="ToolResult{T}"/>, a success returns its value
/// and a failure returns an MCP error result carrying the failure's code and message.
/// Requires the Mythetech.Framework.AI.Generator package to generate the MCP tool implementation.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ToolRequestAttribute : Attribute
{
    /// <summary>
    /// Optional override for the tool name. If not specified, derived from the type name
    /// using snake_case conversion (e.g., AddReposToWorkspace becomes add_repos_to_workspace).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional override for the tool description. If not specified, derived from the
    /// type's XML summary documentation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The type of the response returned by the request handler. Required.
    /// Used by the source generator to generate the correct SendAsync call.
    /// </summary>
    public Type? ResponseType { get; set; }
}
