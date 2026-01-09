using System.Net;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Mcp;
using Mythetech.Framework.Infrastructure.Mcp.Protocol.JsonRpc;
using Mythetech.Framework.Infrastructure.Mcp.Server;
using Mythetech.Framework.Infrastructure.Mcp.Transport;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Mcp;

[UnsupportedOSPlatform("browser")]
public class HttpMcpTransportTests : IAsyncDisposable
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private HttpMcpTransport? _transport;
    private HttpClient? _httpClient;
    private string? _sessionId;

    public async ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
        if (_transport != null)
        {
            await _transport.DisposeAsync();
        }
    }

    [Fact(DisplayName = "HTTP transport starts and listens on configured port")]
    public async Task StartsOnConfiguredPort()
    {
        // Arrange
        var options = new McpServerOptions
        {
            HttpPort = 33330,
            HttpPath = "/mcp"
        };
        _transport = new HttpMcpTransport(options);

        // Act
        await _transport.StartAsync();

        // Assert
        _transport.Endpoint.ShouldNotBeNull();
        _transport.Endpoint.ShouldBe("http://localhost:33330/mcp");
        _transport.ActualPort.ShouldBe(33330);
    }

    [Fact(DisplayName = "HTTP transport falls back to another port when configured port is in use")]
    public async Task FallsBackToAnotherPort()
    {
        // Arrange - Start first transport on configured port
        var options1 = new McpServerOptions
        {
            HttpPort = 33331,
            HttpPath = "/mcp"
        };
        var transport1 = new HttpMcpTransport(options1);
        await transport1.StartAsync();

        try
        {
            // Arrange - Try to start second transport on same port
            var options2 = new McpServerOptions
            {
                HttpPort = 33331,
                HttpPath = "/mcp"
            };
            _transport = new HttpMcpTransport(options2);

            // Act
            await _transport.StartAsync();

            // Assert - Should fall back to port + 1
            _transport.ActualPort.ShouldNotBe(33331);
            _transport.ActualPort.ShouldBe(33332);
            _transport.Endpoint.ShouldBe("http://localhost:33332/mcp");
        }
        finally
        {
            await transport1.DisposeAsync();
        }
    }

    [Fact(DisplayName = "HTTP transport accepts POST requests with JSON-RPC")]
    public async Task AcceptsPostRequests()
    {
        // Arrange
        var options = new McpServerOptions
        {
            HttpPort = 33333,
            HttpPath = "/mcp"
        };
        _transport = new HttpMcpTransport(options);
        await _transport.StartAsync();

        _httpClient = new HttpClient();

        var request = new JsonRpcRequest
        {
            Id = 1,
            Method = "ping"
        };

        // Start reading in background
        var readTask = _transport.ReadMessageAsync();

        // Act - Send HTTP request
        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var responseTask = _httpClient.PostAsync(_transport.Endpoint, content);

        // Wait for transport to receive the request
        var receivedRequest = await readTask.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        receivedRequest.ShouldNotBeNull();
        receivedRequest.Method.ShouldBe("ping");
        receivedRequest.Id.ShouldNotBeNull();

        // Send response to complete HTTP request
        await _transport.WriteMessageAsync(JsonRpcResponse.Success(receivedRequest.Id, new { }));

        var httpResponse = await responseTask.WaitAsync(TimeSpan.FromSeconds(5));
        httpResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "HTTP transport returns 202 Accepted for notifications")]
    public async Task Returns202ForNotifications()
    {
        // Arrange
        var options = new McpServerOptions
        {
            HttpPort = 33334,
            HttpPath = "/mcp"
        };
        _transport = new HttpMcpTransport(options);
        await _transport.StartAsync();

        _httpClient = new HttpClient();

        // Notification has no id
        var notification = new { jsonrpc = "2.0", method = "initialized" };

        // Act
        var content = new StringContent(
            JsonSerializer.Serialize(notification, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(_transport.Endpoint, content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact(DisplayName = "HTTP transport returns 400 for invalid JSON")]
    public async Task Returns400ForInvalidJson()
    {
        // Arrange
        var options = new McpServerOptions
        {
            HttpPort = 33335,
            HttpPath = "/mcp"
        };
        _transport = new HttpMcpTransport(options);
        await _transport.StartAsync();

        _httpClient = new HttpClient();

        // Act
        var content = new StringContent("not valid json", Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(_transport.Endpoint, content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "HTTP transport returns 405 for GET requests")]
    public async Task Returns405ForGet()
    {
        // Arrange
        var options = new McpServerOptions
        {
            HttpPort = 33336,
            HttpPath = "/mcp"
        };
        _transport = new HttpMcpTransport(options);
        await _transport.StartAsync();

        _httpClient = new HttpClient();

        // Act
        var response = await _httpClient.GetAsync(_transport.Endpoint);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    [Fact(DisplayName = "Full MCP server integration over HTTP")]
    public async Task FullServerIntegration()
    {
        // Arrange
        var options = new McpServerOptions
        {
            HttpPort = 33337,
            HttpPath = "/mcp",
            ServerName = "TestServer",
            ServerVersion = "1.0.0"
        };
        _transport = new HttpMcpTransport(options);

        var registry = new McpToolRegistry(Substitute.For<ILogger<McpToolRegistry>>());
        registry.RegisterTool(new McpToolDescriptor
        {
            Name = "test_tool",
            Description = "A test tool",
            ToolType = typeof(HttpTestTool),
            InputSchema = new Dictionary<string, object> { ["type"] = "object" }
        });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBus>(sp => new InMemoryMessageBus(
            sp,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>()));
        var serviceProvider = services.BuildServiceProvider();

        var server = new McpServer(
            _transport,
            serviceProvider.GetRequiredService<IMessageBus>(),
            registry,
            Options.Create(options),
            Substitute.For<ILogger<McpServer>>());

        await _transport.StartAsync();

        // Run server in background
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var serverTask = Task.Run(() => server.RunAsync(cts.Token));

        _httpClient = new HttpClient();

        // Act - Initialize
        var initRequest = new { jsonrpc = "2.0", id = 1, method = "initialize", @params = new { protocolVersion = "2024-11-05" } };
        var initResponse = await SendJsonRpcRequest(initRequest);

        // Assert initialize response
        initResponse.ShouldNotBeNull();
        initResponse.Error.ShouldBeNull();

        // Act - List tools
        var listRequest = new { jsonrpc = "2.0", id = 2, method = "tools/list" };
        var listResponse = await SendJsonRpcRequest(listRequest);

        // Assert tools/list response
        listResponse.ShouldNotBeNull();
        listResponse.Error.ShouldBeNull();
        var resultJson = JsonSerializer.Serialize(listResponse.Result, _jsonOptions);
        resultJson.ShouldContain("test_tool");

        // Cleanup
        cts.Cancel();
        try { await serverTask; } catch (OperationCanceledException) { }
    }

    [Fact(DisplayName = "HTTP transport allows new client to initialize after existing session")]
    public async Task AllowsNewClientToInitializeAfterExistingSession()
    {
        // Arrange
        var options = new McpServerOptions
        {
            HttpPort = 33338,
            HttpPath = "/mcp",
            ServerName = "TestServer",
            ServerVersion = "1.0.0"
        };
        _transport = new HttpMcpTransport(options);

        var registry = new McpToolRegistry(Substitute.For<ILogger<McpToolRegistry>>());
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBus>(sp => new InMemoryMessageBus(
            sp,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>()));
        var serviceProvider = services.BuildServiceProvider();

        var server = new McpServer(
            _transport,
            serviceProvider.GetRequiredService<IMessageBus>(),
            registry,
            Options.Create(options),
            Substitute.For<ILogger<McpServer>>());

        await _transport.StartAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var serverTask = Task.Run(() => server.RunAsync(cts.Token));

        _httpClient = new HttpClient();

        // Act - First client initializes
        var initRequest1 = new { jsonrpc = "2.0", id = 1, method = "initialize", @params = new { protocolVersion = "2024-11-05" } };
        var initResponse1 = await SendJsonRpcRequest(initRequest1);

        initResponse1.ShouldNotBeNull();
        initResponse1.Error.ShouldBeNull();
        var firstSessionId = _sessionId;
        firstSessionId.ShouldNotBeNull();

        // Act - Second client tries to initialize (simulates new client connection)
        // Clear session ID to simulate a new client without a session
        _sessionId = null;

        var initRequest2 = new { jsonrpc = "2.0", id = 1, method = "initialize", @params = new { protocolVersion = "2024-11-05" } };
        var initResponse2 = await SendJsonRpcRequest(initRequest2);

        // Assert - Second initialize should succeed and get a new session
        initResponse2.ShouldNotBeNull();
        initResponse2.Error.ShouldBeNull();
        _sessionId.ShouldNotBeNull();
        _sessionId.ShouldNotBe(firstSessionId); // Should be a new session

        // Cleanup
        cts.Cancel();
        try { await serverTask; } catch (OperationCanceledException) { }
    }

    [Fact(DisplayName = "HTTP transport rejects non-initialize requests without valid session ID")]
    public async Task RejectsNonInitializeRequestsWithoutValidSession()
    {
        // Arrange
        var options = new McpServerOptions
        {
            HttpPort = 33339,
            HttpPath = "/mcp",
            ServerName = "TestServer",
            ServerVersion = "1.0.0"
        };
        _transport = new HttpMcpTransport(options);

        var registry = new McpToolRegistry(Substitute.For<ILogger<McpToolRegistry>>());
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBus>(sp => new InMemoryMessageBus(
            sp,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>()));
        var serviceProvider = services.BuildServiceProvider();

        var server = new McpServer(
            _transport,
            serviceProvider.GetRequiredService<IMessageBus>(),
            registry,
            Options.Create(options),
            Substitute.For<ILogger<McpServer>>());

        await _transport.StartAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var serverTask = Task.Run(() => server.RunAsync(cts.Token));

        _httpClient = new HttpClient();

        // Act - First client initializes to establish a session
        var initRequest = new { jsonrpc = "2.0", id = 1, method = "initialize", @params = new { protocolVersion = "2024-11-05" } };
        var initResponse = await SendJsonRpcRequest(initRequest);
        initResponse.ShouldNotBeNull();
        initResponse.Error.ShouldBeNull();

        // Clear session ID to simulate a request without valid session
        _sessionId = null;

        // Act - Try to call tools/list without a session ID (should fail)
        var listRequest = new { jsonrpc = "2.0", id = 2, method = "tools/list" };
        var content = new StringContent(
            JsonSerializer.Serialize(listRequest, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.PostAsync(_transport.Endpoint, content);

        // Assert - Should get a 400 error
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Cleanup
        cts.Cancel();
        try { await serverTask; } catch (OperationCanceledException) { }
    }

    private async Task<JsonRpcResponse?> SendJsonRpcRequest(object request)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(request, _jsonOptions),
            Encoding.UTF8,
            "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _transport!.Endpoint);
        httpRequest.Content = content;
        httpRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        // Include session ID if we have one
        if (_sessionId != null)
        {
            httpRequest.Headers.Add("Mcp-Session-Id", _sessionId);
        }

        var response = await _httpClient!.SendAsync(httpRequest);

        // Capture session ID from response if present
        if (response.Headers.TryGetValues("Mcp-Session-Id", out var sessionIds))
        {
            _sessionId = sessionIds.FirstOrDefault();
        }

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonRpcResponse>(json, _jsonOptions);
    }
}

[McpTool(Name = "http_test_tool", Description = "Test tool for HTTP transport tests")]
public class HttpTestTool : IMcpTool
{
    public Task<McpToolResult> ExecuteAsync(object? input, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(McpToolResult.Text("HTTP test result"));
    }
}
