using MudBlazor;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Settings for the MCP (Model Context Protocol) server.
/// Some settings are Desktop-only (HTTP transport settings).
/// </summary>
public class McpSettings : SettingsBase
{
    /// <inheritdoc />
    public override string SettingsId => "Mcp";

    /// <inheritdoc />
    public override string DisplayName => "MCP Server";

    /// <inheritdoc />
    public override string Icon => Icons.Material.Filled.Api;

    /// <inheritdoc />
    public override int Order => 300;

    /// <summary>
    /// Whether to auto-start the MCP server when the application launches.
    /// </summary>
    [Setting(
        Label = "Auto-Start Server",
        Description = "Automatically start the MCP server when the application launches",
        Group = "Server",
        Order = 10)]
    public bool AutoStart { get; set; }

    /// <summary>
    /// Timeout for tool execution in seconds.
    /// </summary>
    [Setting(
        Label = "Tool Timeout (seconds)",
        Description = "Maximum time allowed for a tool to execute before timing out",
        Group = "Server",
        Order = 20,
        Min = 5,
        Max = 300,
        Step = 5)]
    public int ToolTimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Enable HTTP transport for MCP server (Desktop only).
    /// </summary>
    [Setting(
        Label = "Enable HTTP Transport",
        Description = "Enable HTTP transport for MCP connections (Desktop only, requires restart)",
        Group = "HTTP Transport",
        Order = 30)]
    public bool HttpEnabled { get; set; }

    /// <summary>
    /// Port for HTTP transport (Desktop only).
    /// </summary>
    [Setting(
        Label = "HTTP Port",
        Description = "Port number for HTTP transport (requires restart)",
        Group = "HTTP Transport",
        Order = 40,
        Min = 1024,
        Max = 65535,
        Step = 1)]
    public int HttpPort { get; set; } = 3333;
}
