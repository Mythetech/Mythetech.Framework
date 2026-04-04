namespace Mythetech.Framework.AI.Generator.Models;

/// <summary>
/// Metadata extracted from a type decorated with [ToolCommand] or [ToolQuery].
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
        bool isQuery = false,
        string? responseTypeName = null)
    {
        TypeName = typeName;
        FullTypeName = fullTypeName;
        Namespace = ns;
        ToolName = toolName;
        Description = description;
        Parameters = parameters;
        IsQuery = isQuery;
        ResponseTypeName = responseTypeName;
    }

    public string TypeName { get; }
    public string FullTypeName { get; }
    public string Namespace { get; }
    public string ToolName { get; }
    public string Description { get; }
    public List<ParameterMetadata> Parameters { get; }
    public bool IsQuery { get; }
    public string? ResponseTypeName { get; }
}

/// <summary>
/// Metadata for a single parameter of a command or query.
/// </summary>
internal sealed class ParameterMetadata
{
    public ParameterMetadata(
        string name,
        string typeName,
        string description,
        bool isRequired,
        bool hasDefaultValue,
        object? defaultValue)
    {
        Name = name;
        TypeName = typeName;
        Description = description;
        IsRequired = isRequired;
        HasDefaultValue = hasDefaultValue;
        DefaultValue = defaultValue;
    }

    public string Name { get; }
    public string TypeName { get; }
    public string Description { get; }
    public bool IsRequired { get; }
    public bool HasDefaultValue { get; }
    public object? DefaultValue { get; }
}
