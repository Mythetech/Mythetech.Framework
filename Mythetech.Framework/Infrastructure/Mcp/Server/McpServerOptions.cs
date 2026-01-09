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

    /// <summary>
    /// Enable HTTP transport for MCP server.
    /// When enabled, the server will listen on the configured HTTP endpoint.
    /// </summary>
    public bool HttpEnabled { get; set; } = false;

    /// <summary>
    /// Port for HTTP transport (default: 3333)
    /// </summary>
    public int HttpPort { get; set; } = 3333;

    /// <summary>
    /// Path for HTTP transport endpoint (default: "/mcp")
    /// </summary>
    public string HttpPath { get; set; } = "/mcp";

    /// <summary>
    /// Host to bind HTTP server to (default: "localhost").
    /// For security, this should always be localhost to prevent DNS rebinding attacks.
    /// </summary>
    public string HttpHost { get; set; } = "localhost";
}
