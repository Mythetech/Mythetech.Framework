using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Calls a registered MCP tool by name. The MCP server reaches tools through this, and code that
/// offers the same tools some other way, such as to a chat client, can call it directly without
/// an MCP transport or the message bus.
/// </summary>
/// <remarks>
/// An unknown or disabled tool, and any exception the tool throws, come back as an error result
/// rather than an exception, so callers treat every outcome the same way.
/// </remarks>
public class McpToolInvoker
{
    private static readonly JsonSerializerOptions ArgumentJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly McpToolRegistry _registry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<McpToolInvoker> _logger;

    /// <summary>
    /// Creates a new tool invoker.
    /// </summary>
    public McpToolInvoker(
        McpToolRegistry registry,
        IServiceProvider serviceProvider,
        ILogger<McpToolInvoker> logger)
    {
        _registry = registry;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Calls the tool with JSON arguments shaped by its input schema.
    /// </summary>
    /// <param name="toolName">The tool's registered name</param>
    /// <param name="arguments">The arguments as a JSON object, or null for none</param>
    /// <param name="cancellationToken">Cancellation token passed to the tool</param>
    public async Task<McpToolResult> InvokeAsync(string toolName, JsonElement? arguments, CancellationToken cancellationToken = default)
    {
        using var activity = McpTelemetry.Source.StartActivity($"Tool:{toolName}");
        activity?.SetTag(McpTelemetry.Tags.ToolName, toolName);

        var descriptor = _registry.GetTool(toolName);
        if (descriptor is null)
        {
            activity?.SetTag(McpTelemetry.Tags.Success, false);
            activity?.SetTag(McpTelemetry.Tags.ErrorMessage, "Tool not found");

            _logger.LogWarning("Unknown tool requested: {ToolName}", toolName);
            return McpToolResult.Error($"Unknown tool: {toolName}");
        }

        if (!_registry.IsToolEnabled(toolName))
        {
            activity?.SetTag(McpTelemetry.Tags.Success, false);
            activity?.SetTag(McpTelemetry.Tags.ErrorMessage, "Tool disabled");

            _logger.LogWarning("Disabled tool requested: {ToolName}", toolName);
            return McpToolResult.Error($"Tool '{toolName}' is disabled");
        }

        try
        {
            var tool = (IMcpTool)_serviceProvider.GetRequiredService(descriptor.ToolType);

            // A call without arguments gets the input type's defaults, so tools whose
            // parameters are all optional can be called bare
            object? input = null;
            if (descriptor.InputType is not null)
            {
                input = arguments is { ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) } json
                    ? json.Deserialize(descriptor.InputType, ArgumentJsonOptions)
                    : Activator.CreateInstance(descriptor.InputType);
            }

            var result = await tool.ExecuteAsync(input, cancellationToken);

            activity?.SetTag(McpTelemetry.Tags.Success, !result.IsError);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", toolName);
            activity?.SetTag(McpTelemetry.Tags.Success, false);
            activity?.SetTag(McpTelemetry.Tags.ErrorMessage, ex.Message);

            return McpToolResult.Error($"Tool execution failed: {ex.Message}");
        }
    }
}
