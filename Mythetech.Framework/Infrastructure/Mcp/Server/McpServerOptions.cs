namespace Mythetech.Framework.Infrastructure.Mcp.Server;

/// <summary>
/// Configuration options for the MCP server.
/// </summary>
public class McpServerOptions
{
    /// <summary>
    /// Server name reported in initialize response
    /// </summary>
    public string ServerName { get; set; } = "Mythetech.Framework";

    /// <summary>
    /// Server version reported in initialize response
    /// </summary>
    public string? ServerVersion { get; set; }

    /// <summary>
    /// MCP protocol version supported
    /// </summary>
    public string ProtocolVersion { get; set; } = "2024-11-05";

    /// <summary>
    /// Timeout for tool execution
    /// </summary>
    public TimeSpan ToolTimeout { get; set; } = TimeSpan.FromSeconds(60);
}
