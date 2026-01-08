using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Runtime registry of available MCP tools.
/// </summary>
public class McpToolRegistry
{
    private readonly Dictionary<string, McpToolDescriptor> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<McpToolRegistry> _logger;

    /// <summary>
    /// Creates a new instance of the tool registry.
    /// </summary>
    public McpToolRegistry(ILogger<McpToolRegistry> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a tool descriptor
    /// </summary>
    public void RegisterTool(McpToolDescriptor descriptor)
    {
        if (_tools.ContainsKey(descriptor.Name))
        {
            _logger.LogWarning("Tool {Name} already registered, overwriting", descriptor.Name);
        }

        _tools[descriptor.Name] = descriptor;
        _logger.LogDebug("Registered MCP tool: {Name}", descriptor.Name);
    }

    /// <summary>
    /// Get a tool by name
    /// </summary>
    public McpToolDescriptor? GetTool(string name)
        => _tools.TryGetValue(name, out var tool) ? tool : null;

    /// <summary>
    /// Get all registered tools
    /// </summary>
    public IReadOnlyList<McpToolDescriptor> GetAllTools()
        => _tools.Values.ToList();

    /// <summary>
    /// Check if a tool is registered
    /// </summary>
    public bool HasTool(string name)
        => _tools.ContainsKey(name);

    /// <summary>
    /// Get the count of registered tools
    /// </summary>
    public int Count => _tools.Count;
}
