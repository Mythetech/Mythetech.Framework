using Mythetech.Framework.Infrastructure.MessageBus;

namespace Mythetech.Framework.Infrastructure.Mcp.Messages;

/// <summary>
/// IQueryHandler that routes MCP tool calls to the appropriate IMcpTool through <see cref="McpToolInvoker"/>.
/// </summary>
public class McpToolCallHandler : IQueryHandler<McpToolCallMessage, McpToolCallResponse>
{
    private readonly McpToolInvoker _invoker;

    /// <summary>
    /// Creates a new instance of the tool call handler.
    /// </summary>
    public McpToolCallHandler(McpToolInvoker invoker)
    {
        _invoker = invoker;
    }

    /// <inheritdoc />
    public async Task<McpToolCallResponse> Handle(McpToolCallMessage message)
    {
        return new McpToolCallResponse
        {
            ToolName = message.ToolName,
            Result = await _invoker.InvokeAsync(message.ToolName, message.Arguments)
        };
    }
}
