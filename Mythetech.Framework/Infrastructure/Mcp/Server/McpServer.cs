using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Mcp.Messages;
using Mythetech.Framework.Infrastructure.Mcp.Protocol.JsonRpc;
using Mythetech.Framework.Infrastructure.Mcp.Protocol.Messages;
using Mythetech.Framework.Infrastructure.Mcp.Transport;

namespace Mythetech.Framework.Infrastructure.Mcp.Server;

/// <summary>
/// Main MCP server implementation.
/// Handles JSON-RPC messages and routes tool calls through MessageBus.
/// </summary>
public class McpServer : IMcpServer
{
    private readonly IMcpTransport _transport;
    private readonly IMessageBus _messageBus;
    private readonly McpToolRegistry _registry;
    private readonly McpServerOptions _options;
    private readonly ILogger<McpServer> _logger;

    /// <summary>
    /// Creates a new MCP server instance.
    /// </summary>
    public McpServer(
        IMcpTransport transport,
        IMessageBus messageBus,
        McpToolRegistry registry,
        IOptions<McpServerOptions> options,
        ILogger<McpServer> logger)
    {
        _transport = transport;
        _messageBus = messageBus;
        _registry = registry;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("MCP server starting: {ServerName} v{Version}",
            _options.ServerName, _options.ServerVersion ?? "1.0.0");

        while (!cancellationToken.IsCancellationRequested)
        {
            JsonRpcRequest? request = null;
            try
            {
                request = await _transport.ReadMessageAsync(cancellationToken);
                if (request is null)
                {
                    _logger.LogInformation("Transport closed, shutting down MCP server");
                    break;
                }

                var response = await ProcessRequestAsync(request, cancellationToken);

                // Only send response for requests (not notifications)
                if (!request.IsNotification && response is not null)
                {
                    await _transport.WriteMessageAsync(response, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("MCP server cancelled");
                break;
            }
            catch (JsonException ex)
            {
                // Malformed JSON in request - send error response if we have a request ID
                _logger.LogWarning(ex, "Invalid JSON in MCP request");
                await TrySendErrorResponseAsync(request?.Id, JsonRpcError.ParseError, "Invalid JSON", cancellationToken);
            }
            catch (Exception ex)
            {
                // Unexpected error - log and continue processing requests
                _logger.LogError(ex, "Error processing MCP request");
                await TrySendErrorResponseAsync(request?.Id, JsonRpcError.InternalError, "Internal server error", cancellationToken);
            }
        }
    }

    private async Task TrySendErrorResponseAsync(object? requestId, int errorCode, string message, CancellationToken cancellationToken)
    {
        if (requestId is null)
        {
            // No request ID means we can't send a response (likely a notification or parse failure)
            return;
        }

        try
        {
            var errorResponse = JsonRpcResponse.Failure(requestId, errorCode, message);
            await _transport.WriteMessageAsync(errorResponse, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send error response to MCP client");
        }
    }

    private async Task<JsonRpcResponse?> ProcessRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken)
    {
        using var activity = McpTelemetry.Source.StartActivity($"RPC:{request.Method}");
        activity?.SetTag(McpTelemetry.Tags.Method, request.Method);
        activity?.SetTag(McpTelemetry.Tags.RequestId, request.Id?.ToString());

        _logger.LogDebug("Processing MCP request: {Method} (id: {Id})", request.Method, request.Id);

        try
        {
            return request.Method switch
            {
                "initialize" => HandleInitialize(request),
                "initialized" => null, // Notification, no response
                "notifications/initialized" => null, // Alternative notification format
                "tools/list" => HandleToolsList(request),
                "tools/call" => await HandleToolsCallAsync(request, cancellationToken),
                "ping" => HandlePing(request),
                _ => JsonRpcResponse.Failure(request.Id, JsonRpcError.MethodNotFound, $"Unknown method: {request.Method}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request {Method}", request.Method);
            activity?.SetTag(McpTelemetry.Tags.Success, false);
            activity?.SetTag(McpTelemetry.Tags.ErrorMessage, ex.Message);
            return JsonRpcResponse.Failure(request.Id, JsonRpcError.InternalError, ex.Message);
        }
    }

    private JsonRpcResponse HandleInitialize(JsonRpcRequest request)
    {
        _logger.LogInformation("MCP client initializing");

        var result = new McpInitializeResult
        {
            ProtocolVersion = _options.ProtocolVersion,
            Capabilities = new McpServerCapabilities
            {
                Tools = new McpToolsCapability { ListChanged = true }
            },
            ServerInfo = new McpServerInfo
            {
                Name = _options.ServerName,
                Version = _options.ServerVersion ?? "1.0.0"
            }
        };

        return JsonRpcResponse.Success(request.Id, result);
    }

    private JsonRpcResponse HandleToolsList(JsonRpcRequest request)
    {
        var tools = _registry.GetEnabledTools()
            .Select(t => new McpToolDefinition
            {
                Name = t.Name,
                Description = t.Description,
                InputSchema = t.InputSchema
            })
            .ToList();

        _logger.LogDebug("Returning {Count} enabled tools", tools.Count);

        var result = new McpToolsListResult { Tools = tools };
        return JsonRpcResponse.Success(request.Id, result);
    }

    private async Task<JsonRpcResponse> HandleToolsCallAsync(JsonRpcRequest request, CancellationToken cancellationToken)
    {
        var callParams = request.Params?.Deserialize<McpToolCallParams>();
        if (callParams is null)
        {
            return JsonRpcResponse.Failure(request.Id, JsonRpcError.InvalidParams, "Missing tool call parameters");
        }

        _logger.LogInformation("Executing tool: {ToolName}", callParams.Name);

        // Route through MessageBus
        var message = new McpToolCallMessage
        {
            ToolName = callParams.Name,
            Arguments = callParams.Arguments,
            RequestId = request.Id
        };

        var config = new QueryConfiguration
        {
            Timeout = _options.ToolTimeout,
            CancellationToken = cancellationToken
        };

        var response = await _messageBus.SendAsync<McpToolCallMessage, McpToolCallResponse>(message, config);

        var result = new McpToolCallResult
        {
            Content = response.Result.Content.Select(c => new McpContentItem
            {
                Type = c.Type,
                Text = c is McpTextContent tc ? tc.Text : null
            }).ToList(),
            IsError = response.Result.IsError ? true : null
        };

        return JsonRpcResponse.Success(request.Id, result);
    }

    private JsonRpcResponse HandlePing(JsonRpcRequest request)
    {
        return JsonRpcResponse.Success(request.Id, new { });
    }

    /// <inheritdoc />
    public async Task NotifyToolsListChangedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending tools/list_changed notification");
        await _transport.WriteNotificationAsync("notifications/tools/list_changed", null, cancellationToken);
    }
}
