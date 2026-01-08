using System.Reflection;
using Microsoft.Extensions.Options;
using Mythetech.Framework.Infrastructure.Mcp.Server;

namespace Mythetech.Framework.Infrastructure.Mcp.Tools;

/// <summary>
/// Built-in MCP tool that returns information about the application.
/// Automatically registered when using the MCP framework.
/// </summary>
[McpTool(Name = "get_app_info", Description = "Returns information about the application including name, version, and runtime details")]
public class GetAppInfoTool : IMcpTool
{
    private readonly McpServerOptions _options;

    /// <summary>
    /// Creates a new instance of the GetAppInfoTool.
    /// </summary>
    public GetAppInfoTool(IOptions<McpServerOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public Task<McpToolResult> ExecuteAsync(object? input, CancellationToken cancellationToken = default)
    {
        var entryAssembly = Assembly.GetEntryAssembly();
        var version = _options.ServerVersion
            ?? entryAssembly?.GetName().Version?.ToString()
            ?? "1.0.0";

        var info = $"""
            Application: {_options.ServerName}
            Version: {version}
            Runtime: .NET {System.Environment.Version}
            OS: {System.Environment.OSVersion}
            Machine: {System.Environment.MachineName}
            """;

        return Task.FromResult(McpToolResult.Text(info));
    }
}
