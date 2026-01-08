using Mythetech.Framework.Infrastructure.MessageBus;

namespace Mythetech.Framework.Infrastructure.Mcp.Messages;

/// <summary>
/// Handles EnableMcpServerMessage by starting the MCP server.
/// </summary>
public class EnableMcpServerConsumer : IConsumer<EnableMcpServerMessage>
{
    private readonly McpServerState _state;

    /// <summary>
    /// Creates a new instance of the consumer.
    /// </summary>
    public EnableMcpServerConsumer(McpServerState state)
    {
        _state = state;
    }

    /// <inheritdoc />
    public async Task Consume(EnableMcpServerMessage message)
    {
        await _state.StartAsync();
    }
}

/// <summary>
/// Handles DisableMcpServerMessage by stopping the MCP server.
/// </summary>
public class DisableMcpServerConsumer : IConsumer<DisableMcpServerMessage>
{
    private readonly McpServerState _state;

    /// <summary>
    /// Creates a new instance of the consumer.
    /// </summary>
    public DisableMcpServerConsumer(McpServerState state)
    {
        _state = state;
    }

    /// <inheritdoc />
    public async Task Consume(DisableMcpServerMessage message)
    {
        await _state.StopAsync();
    }
}
