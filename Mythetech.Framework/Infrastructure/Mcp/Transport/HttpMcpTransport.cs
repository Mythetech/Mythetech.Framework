using System.Collections.Concurrent;
using System.Net;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Mcp.Protocol.JsonRpc;
using Mythetech.Framework.Infrastructure.Mcp.Server;

namespace Mythetech.Framework.Infrastructure.Mcp.Transport;

/// <summary>
/// MCP transport over HTTP implementing the "Streamable HTTP" transport spec.
/// Listens on a configurable local port and handles JSON-RPC messages over HTTP POST.
/// Note: HttpListener is not supported on browser platform.
/// </summary>
[UnsupportedOSPlatform("browser")]
public class HttpMcpTransport : IMcpTransport
{
    private HttpListener? _listener;
    private readonly McpServerOptions _options;
    private readonly ILogger<HttpMcpTransport>? _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly CancellationTokenSource _cts = new();

    // Queue for incoming requests (HTTP requests are queued, ReadMessageAsync dequeues)
    private readonly BlockingCollection<JsonRpcRequest> _requestQueue = new();

    // Pending responses waiting to be sent back to HTTP clients, keyed by transport-wide request id
    private readonly ConcurrentDictionary<long, PendingRequest> _pendingResponses = new();
    private long _lastRequestId;

    // Active sessions per MCP spec. Each client that initializes gets its own session.
    private readonly ConcurrentDictionary<string, byte> _sessions = new();

    private Task? _listenerTask;
    private bool _disposed;

    /// <summary>
    /// The actual endpoint URL the server is listening on.
    /// This may differ from the configured port if port fallback was used.
    /// </summary>
    public string? Endpoint { get; private set; }

    /// <summary>
    /// The actual port the server is listening on.
    /// </summary>
    public int? ActualPort { get; private set; }

    /// <summary>
    /// Creates a new HTTP MCP transport instance.
    /// </summary>
    /// <param name="options">MCP server configuration options</param>
    /// <param name="logger">Optional logger instance</param>
    public HttpMcpTransport(McpServerOptions options, ILogger<HttpMcpTransport>? logger = null)
    {
        _options = options;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Starts the HTTP listener. Attempts to bind to the configured port,
    /// falling back to alternative ports if the configured port is in use.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var port = _options.HttpPort;
        var host = _options.HttpHost;
        var path = _options.HttpPath.TrimStart('/');

        // Try configured port first, then fall back to alternatives
        var portsToTry = new[] { port, port + 1, port + 2, port + 10, 0 };

        foreach (var tryPort in portsToTry)
        {
            // Create a new listener for each attempt (HttpListener becomes unusable after failed Start)
            var listener = new HttpListener();
            try
            {
                var prefix = $"http://{host}:{tryPort}/{path}/";
                listener.Prefixes.Add(prefix);
                listener.Start();

                _listener = listener;
                ActualPort = tryPort;
                Endpoint = $"http://{host}:{tryPort}/{path}";

                if (tryPort != port)
                {
                    _logger?.LogWarning("Port {ConfiguredPort} was unavailable, using port {ActualPort} instead",
                        port, tryPort);
                }

                _logger?.LogInformation("MCP HTTP transport listening on {Endpoint}", Endpoint);

                // Start accepting requests in background
                _listenerTask = AcceptRequestsAsync(_cts.Token);
                return;
            }
            catch (HttpListenerException ex) when (IsPortUnavailableException(ex))
            {
                _logger?.LogDebug("Port {Port} is unavailable (ErrorCode: {ErrorCode}), trying next port", tryPort, ex.ErrorCode);
                listener.Close();
                continue;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start HTTP listener on port {Port}", tryPort);
                listener.Close();
                throw;
            }
        }

        throw new InvalidOperationException($"Unable to start HTTP listener - all ports from {port} are in use");
    }

    private async Task AcceptRequestsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener?.IsListening == true)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(cancellationToken);
                _ = Task.Run(() => HandleRequestAsync(context, cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error accepting HTTP request");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            // Security: Validate Origin header to prevent DNS rebinding attacks
            var origin = request.Headers["Origin"];
            if (!string.IsNullOrEmpty(origin))
            {
                var originUri = new Uri(origin);
                if (originUri.Host != "localhost" && originUri.Host != "127.0.0.1")
                {
                    _logger?.LogWarning("Rejected request from non-localhost origin: {Origin}", origin);
                    response.StatusCode = 403;
                    response.Close();
                    return;
                }
            }

            if (request.HttpMethod == "POST")
            {
                await HandlePostAsync(context, cancellationToken);
            }
            else if (request.HttpMethod == "GET")
            {
                // GET is for SSE streaming (optional per spec)
                response.StatusCode = 405;
                response.Headers["Allow"] = "POST";
                response.Close();
            }
            else if (request.HttpMethod == "DELETE")
            {
                await HandleDeleteAsync(context);
            }
            else if (request.HttpMethod == "OPTIONS")
            {
                // CORS preflight
                response.Headers["Access-Control-Allow-Origin"] = "http://localhost";
                response.Headers["Access-Control-Allow-Methods"] = "POST, GET, DELETE, OPTIONS";
                response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Accept, Mcp-Session-Id";
                response.StatusCode = 204;
                response.Close();
            }
            else
            {
                response.StatusCode = 405;
                response.Close();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling HTTP request");
            try
            {
                response.StatusCode = 500;
                response.Close();
            }
            catch (Exception closeEx)
            {
                _logger?.LogDebug(closeEx, "Failed to close response after error");
            }
        }
    }

    private async Task HandlePostAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var request = context.Request;
        var response = context.Response;

        // Read request body
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(body))
        {
            response.StatusCode = 400;
            await WriteJsonResponse(response, new JsonRpcResponse
            {
                Id = null,
                Error = new JsonRpcError { Code = JsonRpcError.InvalidRequest, Message = "Empty request body" }
            });
            return;
        }

        try
        {
            // Parse JSON-RPC request
            var jsonRpcRequest = JsonSerializer.Deserialize<JsonRpcRequest>(body, _jsonOptions);

            if (jsonRpcRequest == null)
            {
                response.StatusCode = 400;
                await WriteJsonResponse(response, new JsonRpcResponse
                {
                    Id = null,
                    Error = new JsonRpcError { Code = JsonRpcError.ParseError, Message = "Failed to parse JSON-RPC request" }
                });
                return;
            }

            // An initialize always starts a new session, so a new client never ends another client's session
            var isInitialize = jsonRpcRequest.Method == "initialize";
            var sessionCheck = CheckSession(request.Headers["Mcp-Session-Id"], isInitialize);
            if (sessionCheck != SessionCheck.Allowed)
            {
                await RejectSessionAsync(response, jsonRpcRequest.Id, sessionCheck);
                return;
            }

            // Handle notifications (no response needed)
            if (jsonRpcRequest.IsNotification)
            {
                // Queue for processing but don't wait for response
                _requestQueue.Add(jsonRpcRequest, cancellationToken);
                response.StatusCode = 202;
                response.Close();
                return;
            }

            // Each client numbers its own requests, so two sessions can both send id 1. The server
            // sees a transport-wide id instead, and the client's id is put back on the response.
            var requestId = Interlocked.Increment(ref _lastRequestId);
            var pending = new PendingRequest(jsonRpcRequest.Id, response, isInitialize);

            // Register pending response before queuing
            _pendingResponses[requestId] = pending;

            try
            {
                // Queue for processing by McpServer
                _requestQueue.Add(new JsonRpcRequest
                {
                    JsonRpc = jsonRpcRequest.JsonRpc,
                    Id = requestId,
                    Method = jsonRpcRequest.Method,
                    Params = jsonRpcRequest.Params
                }, cancellationToken);

                // Wait for response to be written (with timeout)
                var timeout = _options.ToolTimeout.Add(TimeSpan.FromSeconds(5));
                await pending.CompletionSource.Task.WaitAsync(timeout, cancellationToken);
            }
            finally
            {
                _pendingResponses.TryRemove(requestId, out _);
            }
        }
        catch (JsonException ex)
        {
            _logger?.LogWarning(ex, "Invalid JSON in request");
            response.StatusCode = 400;
            await WriteJsonResponse(response, new JsonRpcResponse
            {
                Id = null,
                Error = new JsonRpcError { Code = JsonRpcError.ParseError, Message = ex.Message }
            });
        }
        catch (TimeoutException)
        {
            _logger?.LogWarning("Request timed out");
            response.StatusCode = 504;
            response.Close();
        }
    }

    private async Task HandleDeleteAsync(HttpListenerContext context)
    {
        var response = context.Response;
        var sessionId = context.Request.Headers["Mcp-Session-Id"];

        var sessionCheck = CheckSession(sessionId, isInitialize: false);
        if (sessionCheck != SessionCheck.Allowed)
        {
            await RejectSessionAsync(response, null, sessionCheck);
            return;
        }

        if (!string.IsNullOrEmpty(sessionId))
        {
            _sessions.TryRemove(sessionId, out _);
            _logger?.LogInformation("MCP session {SessionId} ended", sessionId);
        }

        response.StatusCode = 204;
        response.Close();
    }

    private SessionCheck CheckSession(string? sessionId, bool isInitialize)
    {
        if (isInitialize)
            return SessionCheck.Allowed;

        if (!string.IsNullOrEmpty(sessionId))
            return _sessions.ContainsKey(sessionId) ? SessionCheck.Allowed : SessionCheck.Unknown;

        // Requests without a session are accepted until some client has initialized
        return _sessions.IsEmpty ? SessionCheck.Allowed : SessionCheck.Missing;
    }

    private async Task RejectSessionAsync(HttpListenerResponse response, object? requestId, SessionCheck sessionCheck)
    {
        // Per the MCP spec, a missing session ID is a 400 and an unknown or ended session is a 404,
        // which tells the client to initialize again
        var isMissing = sessionCheck == SessionCheck.Missing;
        var message = isMissing ? "Missing session ID" : "Session not found";

        _logger?.LogWarning("Rejected MCP request: {Reason}", message);
        response.StatusCode = isMissing ? 400 : 404;
        await WriteJsonResponse(response, new JsonRpcResponse
        {
            Id = requestId,
            Error = new JsonRpcError { Code = JsonRpcError.InvalidRequest, Message = message }
        });
    }

    /// <inheritdoc />
    public async Task<JsonRpcRequest?> ReadMessageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Wait for a request to be queued from HTTP
            return await Task.Run(() =>
            {
                try
                {
                    return _requestQueue.Take(cancellationToken);
                }
                catch (InvalidOperationException)
                {
                    return null; // Queue completed
                }
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task WriteMessageAsync(JsonRpcResponse response, CancellationToken cancellationToken = default)
    {
        // Find the pending HTTP request for this response
        if (response.Id is long requestId && _pendingResponses.TryRemove(requestId, out var pending))
        {
            try
            {
                if (pending.IsInitialize && response.Result != null)
                {
                    var sessionId = Guid.NewGuid().ToString("N");
                    _sessions[sessionId] = 0;
                    pending.Response.Headers["Mcp-Session-Id"] = sessionId;
                    _logger?.LogInformation("MCP session {SessionId} started", sessionId);
                }

                pending.Response.ContentType = "application/json";
                pending.Response.StatusCode = 200;

                await WriteJsonResponse(pending.Response, new JsonRpcResponse
                {
                    JsonRpc = response.JsonRpc,
                    Id = pending.ClientRequestId,
                    Result = response.Result,
                    Error = response.Error
                });
                pending.CompletionSource.TrySetResult(true);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to write HTTP response");
                pending.CompletionSource.TrySetException(ex);
            }
        }
    }

    /// <inheritdoc />
    public Task WriteNotificationAsync(string method, object? @params, CancellationToken cancellationToken = default)
    {
        // For HTTP transport, server-initiated notifications would require SSE
        // Currently we don't support that, so just log and continue
        _logger?.LogDebug("Server notification {Method} not sent (SSE not implemented)", method);
        return Task.CompletedTask;
    }

    private async Task WriteJsonResponse(HttpListenerResponse response, object data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    private static bool IsPortUnavailableException(HttpListenerException ex)
    {
        // Error codes for port/address unavailable across different platforms:
        // macOS: 48 (EADDRINUSE), 5 (conflicts with existing registration)
        // Windows: 183 (ERROR_ALREADY_EXISTS), 5 (ERROR_ACCESS_DENIED)
        // Linux: 98 (EADDRINUSE)
        // Also catch by message for robustness
        return ex.ErrorCode is 48 or 183 or 98 or 5 ||
               ex.Message.Contains("conflicts with an existing registration", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("address already in use", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Stops the HTTP listener, allowing it to be restarted later with StartAsync.
    /// Unlike DisposeAsync, this method does not dispose internal resources.
    /// </summary>
    public async Task StopAsync()
    {
        if (_listener == null || !_listener.IsListening)
            return;

        _logger?.LogInformation("Stopping MCP HTTP transport...");

        // Complete all pending requests with cancellation
        foreach (var pending in _pendingResponses.Values)
        {
            pending.CompletionSource.TrySetCanceled();
        }
        _pendingResponses.Clear();

        try
        {
            _listener.Stop();
            _listener.Close();
            _listener = null;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Error closing HTTP listener during stop");
        }

        if (_listenerTask != null)
        {
            try
            {
                await _listenerTask.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Timeout waiting for listener task during stop");
            }
            _listenerTask = null;
        }

        // Reset state for restart
        Endpoint = null;
        ActualPort = null;
        _sessions.Clear();

        _logger?.LogInformation("MCP HTTP transport stopped");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _requestQueue.CompleteAdding();

        // Complete all pending requests
        foreach (var pending in _pendingResponses.Values)
        {
            pending.CompletionSource.TrySetCanceled();
        }
        _pendingResponses.Clear();

        try
        {
            _listener?.Stop();
            _listener?.Close();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Error closing HTTP listener during dispose");
        }

        if (_listenerTask != null)
        {
            try
            {
                await _listenerTask.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "Timeout waiting for listener task during dispose");
            }
        }

        _cts.Dispose();
        _requestQueue.Dispose();
    }

    /// <summary>
    /// Represents a pending HTTP request waiting for a JSON-RPC response.
    /// </summary>
    private sealed class PendingRequest
    {
        public object? ClientRequestId { get; }
        public HttpListenerResponse Response { get; }
        public bool IsInitialize { get; }
        public TaskCompletionSource<bool> CompletionSource { get; } = new();

        public PendingRequest(object? clientRequestId, HttpListenerResponse response, bool isInitialize)
        {
            ClientRequestId = clientRequestId;
            Response = response;
            IsInitialize = isInitialize;
        }
    }

    private enum SessionCheck
    {
        Allowed,
        Missing,
        Unknown
    }
}
