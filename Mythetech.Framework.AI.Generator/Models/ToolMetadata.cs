using Microsoft.CodeAnalysis;

namespace Mythetech.Framework.AI.Generator.Models;

/// <summary>
/// Metadata extracted from a type decorated with [ToolCommand] or [ToolRequest].
/// </summary>
internal sealed class ToolMetadata
{
    public ToolMetadata(
        string typeName,
        string fullTypeName,
        string ns,
        string toolName,
        string description,
        List<ParameterMetadata> parameters,
        string attributeName,
        Location? location,
        bool isRequest = false,
        string? responseTypeName = null,
        bool isToolResult = false)
    {
        TypeName = typeName;
        FullTypeName = fullTypeName;
        Namespace = ns;
        ToolName = toolName;
        Description = description;
        Parameters = parameters;
        AttributeName = attributeName;
        Location = location;
        IsRequest = isRequest;
        ResponseTypeName = responseTypeName;
        IsToolResult = isToolResult;
    }

    public string TypeName { get; }
    public string FullTypeName { get; }
    public string Namespace { get; }
    public string ToolName { get; }
    public string Description { get; }
    public List<ParameterMetadata> Parameters { get; }

    /// <summary>
    /// The attribute's name as written, without the Attribute suffix, for diagnostics.
    /// </summary>
    public string AttributeName { get; }

    /// <summary>
    /// Where the attribute is applied, for diagnostics.
    /// </summary>
    public Location? Location { get; }

    /// <summary>
    /// True for [ToolRequest] (and the legacy [ToolQuery]), which are sent and return a response.
    /// </summary>
    public bool IsRequest { get; }

    public string? ResponseTypeName { get; }

    /// <summary>
    /// True when the response is a ToolResult&lt;T&gt;, whose failures become MCP error results.
    /// </summary>
    public bool IsToolResult { get; }
}

/// <summary>
/// Metadata for a single parameter of a command or request.
/// </summary>
internal sealed class ParameterMetadata
{
    public ParameterMetadata(
        string name,
        string typeName,
        string description,
        bool isRequired,
        string? defaultValueLiteral)
    {
        Name = name;
        TypeName = typeName;
        Description = description;
        IsRequired = isRequired;
        DefaultValueLiteral = defaultValueLiteral;
    }

    public string Name { get; }
    public string TypeName { get; }
    public string Description { get; }
    public bool IsRequired { get; }

    /// <summary>
    /// The declared default as a C# expression, or null when the parameter has none.
    /// </summary>
    public string? DefaultValueLiteral { get; }
}
