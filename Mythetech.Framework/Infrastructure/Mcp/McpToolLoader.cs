using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Discovers and loads MCP tools from assemblies.
/// </summary>
public class McpToolLoader
{
    private readonly ILogger<McpToolLoader> _logger;

    /// <summary>
    /// Creates a new instance of the tool loader.
    /// </summary>
    public McpToolLoader(ILogger<McpToolLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Discover tool types from an assembly
    /// </summary>
    public IEnumerable<McpToolDescriptor> DiscoverTools(Assembly assembly)
    {
        var toolTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => typeof(IMcpTool).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttribute<McpToolAttribute>() is not null);

        foreach (var type in toolTypes)
        {
            var attr = type.GetCustomAttribute<McpToolAttribute>()!;
            var inputType = GetInputType(type);

            var descriptor = new McpToolDescriptor
            {
                Name = attr.Name,
                Description = attr.Description,
                ToolType = type,
                InputType = inputType,
                InputSchema = inputType is not null ? GenerateInputSchema(inputType) : GetEmptySchema()
            };

            _logger.LogDebug("Discovered MCP tool {Name} from {Type}", descriptor.Name, type.FullName);
            yield return descriptor;
        }
    }

    private Type? GetInputType(Type toolType)
    {
        // Look for IMcpTool<TInput> implementation
        var genericInterface = toolType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMcpTool<>));

        return genericInterface?.GetGenericArguments()[0];
    }

    private object GetEmptySchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>()
        };
    }

    private object GenerateInputSchema(Type inputType)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var prop in inputType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = prop.GetCustomAttribute<McpToolInputAttribute>();
            var propSchema = new Dictionary<string, object>
            {
                ["type"] = GetJsonType(prop.PropertyType)
            };

            if (attr?.Description is not null)
            {
                propSchema["description"] = attr.Description;
            }

            properties[ToCamelCase(prop.Name)] = propSchema;

            if (attr?.Required == true)
            {
                required.Add(ToCamelCase(prop.Name));
            }
        }

        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = properties
        };

        if (required.Count > 0)
        {
            schema["required"] = required;
        }

        return schema;
    }

    private string GetJsonType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying switch
        {
            Type t when t == typeof(string) => "string",
            Type t when t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte) => "integer",
            Type t when t == typeof(float) || t == typeof(double) || t == typeof(decimal) => "number",
            Type t when t == typeof(bool) => "boolean",
            Type t when t.IsArray || (t.IsGenericType && typeof(IEnumerable).IsAssignableFrom(t)) => "array",
            _ => "object"
        };
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
