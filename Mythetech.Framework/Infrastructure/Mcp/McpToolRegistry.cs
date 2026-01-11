using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Runtime registry of available MCP tools.
/// </summary>
public class McpToolRegistry
{
    private readonly Dictionary<string, McpToolDescriptor> _tools = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _disabledTools = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<McpToolRegistry> _logger;
    private readonly IMcpToolStateProvider? _stateProvider;

    /// <summary>
    /// Creates a new instance of the tool registry.
    /// </summary>
    public McpToolRegistry(ILogger<McpToolRegistry> logger, IMcpToolStateProvider? stateProvider = null)
    {
        _logger = logger;
        _stateProvider = stateProvider;
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

    /// <summary>
    /// Check if a tool is enabled
    /// </summary>
    public bool IsToolEnabled(string name) => !_disabledTools.Contains(name);

    /// <summary>
    /// Set whether a tool is enabled
    /// </summary>
    public async Task SetToolEnabledAsync(string name, bool enabled)
    {
        if (enabled)
        {
            _disabledTools.Remove(name);
            _logger.LogInformation("MCP tool enabled: {Name}", name);
        }
        else
        {
            _disabledTools.Add(name);
            _logger.LogInformation("MCP tool disabled: {Name}", name);
        }

        if (_stateProvider != null)
        {
            await _stateProvider.SaveDisabledToolsAsync(_disabledTools);
        }
    }

    /// <summary>
    /// Get all enabled tools
    /// </summary>
    public IReadOnlyList<McpToolDescriptor> GetEnabledTools()
        => _tools.Values.Where(t => IsToolEnabled(t.Name)).ToList();

    /// <summary>
    /// Load tool enabled/disabled state from the provider (if configured)
    /// </summary>
    public async Task LoadStateAsync()
    {
        if (_stateProvider != null)
        {
            var disabled = await _stateProvider.LoadDisabledToolsAsync();
            foreach (var name in disabled)
            {
                _disabledTools.Add(name);
            }
            _logger.LogInformation("Loaded {Count} disabled tools from state provider", disabled.Count);
        }
    }
}
