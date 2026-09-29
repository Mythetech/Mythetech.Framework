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
            var propSchema = GetTypeSchema(prop.PropertyType);

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

    private static Dictionary<string, object> GetTypeSchema(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string) || underlying == typeof(char))
            return Schema("string");

        if (underlying == typeof(Guid))
            return Schema("string", format: "uuid");

        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset))
            return Schema("string", format: "date-time");

        if (underlying == typeof(DateOnly))
            return Schema("string", format: "date");

        if (underlying == typeof(TimeOnly))
            return Schema("string", format: "time");

        if (underlying == typeof(Uri))
            return Schema("string", format: "uri");

        if (underlying.IsEnum)
        {
            var schema = Schema("string");
            schema["enum"] = Enum.GetNames(underlying);
            return schema;
        }

        if (underlying == typeof(bool))
            return Schema("boolean");

        if (IsInteger(underlying))
            return Schema("integer");

        if (underlying == typeof(float) || underlying == typeof(double) || underlying == typeof(decimal))
            return Schema("number");

        if (IsDictionary(underlying))
            return Schema("object");

        if (GetElementType(underlying) is { } elementType)
        {
            var schema = Schema("array");
            schema["items"] = GetTypeSchema(elementType);
            return schema;
        }

        return Schema("object");
    }

    private static Dictionary<string, object> Schema(string type, string? format = null)
    {
        var schema = new Dictionary<string, object> { ["type"] = type };
        if (format is not null)
        {
            schema["format"] = format;
        }

        return schema;
    }

    private static bool IsInteger(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
        type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte);

    private static bool IsDictionary(Type type) =>
        typeof(IDictionary).IsAssignableFrom(type) ||
        GetGenericInterfaces(type).Any(i =>
            i.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
            i.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));

    private static Type? GetElementType(Type type)
    {
        if (type.IsArray)
            return type.GetElementType();

        return GetGenericInterfaces(type)
            .FirstOrDefault(i => i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    private static IEnumerable<Type> GetGenericInterfaces(Type type)
    {
        var interfaces = type.IsInterface ? type.GetInterfaces().Prepend(type) : type.GetInterfaces();
        return interfaces.Where(i => i.IsGenericType);
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
