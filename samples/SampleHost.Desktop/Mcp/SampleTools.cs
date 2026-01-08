using Mythetech.Framework.Infrastructure.Mcp;

namespace SampleHost.Desktop.Mcp;

/// <summary>
/// Sample MCP tool that echoes back input.
/// </summary>
[McpTool(Name = "echo", Description = "Echoes back the provided message")]
public class EchoTool : IMcpTool<EchoInput>
{
    /// <inheritdoc />
    public Task<McpToolResult> ExecuteAsync(EchoInput input, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(McpToolResult.Text($"Echo: {input.Message}"));
    }
}

/// <summary>
/// Input for the echo tool.
/// </summary>
public class EchoInput
{
    /// <summary>
    /// The message to echo back.
    /// </summary>
    [McpToolInput(Description = "The message to echo back", Required = true)]
    public string Message { get; set; } = "";
}
