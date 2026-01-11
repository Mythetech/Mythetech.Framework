using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;

namespace Mythetech.Framework.Infrastructure.Mcp.Messages;

/// <summary>
/// IQueryHandler that routes MCP tool calls to the appropriate IMcpTool.
/// </summary>
public class McpToolCallHandler : IQueryHandler<McpToolCallMessage, McpToolCallResponse>
{
    private readonly McpToolRegistry _registry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<McpToolCallHandler> _logger;

    /// <summary>
    /// Creates a new instance of the tool call handler.
    /// </summary>
    public McpToolCallHandler(
        McpToolRegistry registry,
        IServiceProvider serviceProvider,
        ILogger<McpToolCallHandler> logger)
    {
        _registry = registry;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<McpToolCallResponse> Handle(McpToolCallMessage message)
    {
        using var activity = McpTelemetry.Source.StartActivity($"Tool:{message.ToolName}");
        activity?.SetTag(McpTelemetry.Tags.ToolName, message.ToolName);

        var descriptor = _registry.GetTool(message.ToolName);
        if (descriptor is null)
        {
            activity?.SetTag(McpTelemetry.Tags.Success, false);
            activity?.SetTag(McpTelemetry.Tags.ErrorMessage, "Tool not found");

            _logger.LogWarning("Unknown tool requested: {ToolName}", message.ToolName);
            return new McpToolCallResponse
            {
                ToolName = message.ToolName,
                Result = McpToolResult.Error($"Unknown tool: {message.ToolName}")
            };
        }

        if (!_registry.IsToolEnabled(message.ToolName))
        {
            activity?.SetTag(McpTelemetry.Tags.Success, false);
            activity?.SetTag(McpTelemetry.Tags.ErrorMessage, "Tool disabled");

            _logger.LogWarning("Disabled tool requested: {ToolName}", message.ToolName);
            return new McpToolCallResponse
            {
                ToolName = message.ToolName,
                Result = McpToolResult.Error($"Tool '{message.ToolName}' is disabled")
            };
        }

        try
        {
            // Resolve tool instance from DI
            var tool = (IMcpTool)_serviceProvider.GetRequiredService(descriptor.ToolType);

            // Deserialize input if the tool has an input type
            object? input = null;
            if (descriptor.InputType is not null && message.Arguments.HasValue)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                input = message.Arguments.Value.Deserialize(descriptor.InputType, jsonOptions);
            }

            var result = await tool.ExecuteAsync(input);

            activity?.SetTag(McpTelemetry.Tags.Success, !result.IsError);

            return new McpToolCallResponse
            {
                ToolName = message.ToolName,
                Result = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool {ToolName}", message.ToolName);
            activity?.SetTag(McpTelemetry.Tags.Success, false);
            activity?.SetTag(McpTelemetry.Tags.ErrorMessage, ex.Message);

            return new McpToolCallResponse
            {
                ToolName = message.ToolName,
                Result = McpToolResult.Error($"Tool execution failed: {ex.Message}")
            };
        }
    }
}
