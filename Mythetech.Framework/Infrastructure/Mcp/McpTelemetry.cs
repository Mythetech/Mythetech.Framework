using System.Diagnostics;
using Mythetech.Framework.Infrastructure.Telemetry;

namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// ActivitySource for MCP operations tracing.
/// </summary>
public static class McpTelemetry
{
    /// <summary>
    /// ActivitySource for MCP server operations.
    /// </summary>
    public static readonly ActivitySource Source = new("Mythetech.Mcp", FrameworkTelemetry.GetVersion());

    /// <summary>
    /// Tag names for MCP telemetry.
    /// </summary>
    public static class Tags
    {
        /// <summary>
        /// The JSON-RPC method being called.
        /// </summary>
        public const string Method = "mcp.method";

        /// <summary>
        /// The name of the tool being executed.
        /// </summary>
        public const string ToolName = "mcp.tool.name";

        /// <summary>
        /// Whether the operation completed successfully.
        /// </summary>
        public const string Success = "mcp.success";

        /// <summary>
        /// Error message if the operation failed.
        /// </summary>
        public const string ErrorMessage = "mcp.error.message";

        /// <summary>
        /// The request ID from the JSON-RPC message.
        /// </summary>
        public const string RequestId = "mcp.request.id";
    }
}
