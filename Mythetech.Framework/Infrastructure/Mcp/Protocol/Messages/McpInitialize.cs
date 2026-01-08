using System.Text.Json.Serialization;

namespace Mythetech.Framework.Infrastructure.Mcp.Protocol.Messages;

/// <summary>
/// Parameters for the initialize request
/// </summary>
public class McpInitializeParams
{
    /// <summary>
    /// The MCP protocol version the client supports
    /// </summary>
    [JsonPropertyName("protocolVersion")]
    public required string ProtocolVersion { get; init; }

    /// <summary>
    /// Client capabilities
    /// </summary>
    [JsonPropertyName("capabilities")]
    public McpClientCapabilities? Capabilities { get; init; }

    /// <summary>
    /// Information about the client
    /// </summary>
    [JsonPropertyName("clientInfo")]
    public McpClientInfo? ClientInfo { get; init; }
}

/// <summary>
/// Client capability declarations
/// </summary>
public class McpClientCapabilities
{
    // Reserved for future client capabilities
}

/// <summary>
/// Information about the MCP client
/// </summary>
public class McpClientInfo
{
    /// <summary>
    /// Client name
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Client version
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }
}

/// <summary>
/// Result of the initialize request
/// </summary>
public class McpInitializeResult
{
    /// <summary>
    /// The MCP protocol version the server supports
    /// </summary>
    [JsonPropertyName("protocolVersion")]
    public required string ProtocolVersion { get; init; }

    /// <summary>
    /// Server capabilities
    /// </summary>
    [JsonPropertyName("capabilities")]
    public required McpServerCapabilities Capabilities { get; init; }

    /// <summary>
    /// Information about the server
    /// </summary>
    [JsonPropertyName("serverInfo")]
    public McpServerInfo? ServerInfo { get; init; }
}

/// <summary>
/// Server capability declarations
/// </summary>
public class McpServerCapabilities
{
    /// <summary>
    /// Tools capability
    /// </summary>
    [JsonPropertyName("tools")]
    public McpToolsCapability? Tools { get; init; }
}

/// <summary>
/// Tools capability declaration
/// </summary>
public class McpToolsCapability
{
    /// <summary>
    /// Whether the server supports tool list change notifications
    /// </summary>
    [JsonPropertyName("listChanged")]
    public bool? ListChanged { get; init; }
}

/// <summary>
/// Information about the MCP server
/// </summary>
public class McpServerInfo
{
    /// <summary>
    /// Server name
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Server version
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }
}
