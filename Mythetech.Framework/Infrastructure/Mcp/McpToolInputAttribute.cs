namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Describes a tool's input parameter for JSON Schema generation.
/// Applied to properties of the input class.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class McpToolInputAttribute : Attribute
{
    /// <summary>
    /// Human-readable description of this parameter.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this parameter is required. Defaults to false.
    /// </summary>
    public bool Required { get; set; } = false;
}
