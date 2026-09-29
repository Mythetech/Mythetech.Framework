using System.Net;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Mcp;
using Mythetech.Framework.Infrastructure.Mcp.Server;
using Mythetech.Framework.Infrastructure.Mcp.Transport;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Mcp;

[UnsupportedOSPlatform("browser")]
public class HttpMcpTransportSessionTests : IAsyncDisposable
{
    private ServiceProvider? _services;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;
    private string _endpoint = "";

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        if (_serverTask != null)
        {
            try { await _serverTask; } catch (OperationCanceledException) { }
        }

        if (_services != null)
        {
            await _services.DisposeAsync();
        }

        _cts?.Dispose();
    }

    [Fact(DisplayName = "Two clients initialize, list tools and call tools in turns without dropping each other")]
    public async Task TwoClientsTakeTurns()
    {
        await StartServerAsync(33340);
        using var first = new TestMcpClient(_endpoint);
        using var second = new TestMcpClient(_endpoint);

        (await first.InitializeAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await second.InitializeAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
        first.SessionId.ShouldNotBeNull();
        second.SessionId.ShouldNotBeNull();
        second.SessionId.ShouldNotBe(first.SessionId);

        foreach (var client in new[] { first, second, first, second })
        {
            var list = await client.SendAsync(2, "tools/list");
            list.StatusCode.ShouldBe(HttpStatusCode.OK);
            list.Body.GetProperty("result").GetRawText().ShouldContain("http_test_tool");
        }

        foreach (var client in new[] { first, second, first, second })
        {
            var call = await client.SendAsync(3, "tools/call", new { name = "http_test_tool" });
            call.StatusCode.ShouldBe(HttpStatusCode.OK);
            call.Body.GetProperty("id").GetInt32().ShouldBe(3);
            call.Body.GetProperty("result").GetRawText().ShouldContain("HTTP test result");
        }
    }

    [Fact(DisplayName = "Concurrent requests with the same id from two clients each get their own response")]
    public async Task SameRequestIdFromTwoClients()
    {
        await StartServerAsync(33341);
        using var first = new TestMcpClient(_endpoint);
        using var second = new TestMcpClient(_endpoint);
        await first.InitializeAsync();
        await second.InitializeAsync();

        var responses = await Task.WhenAll(
            first.SendAsync(7, "tools/call", new { name = "http_test_tool" }),
            second.SendAsync(7, "ping"));

        responses[0].StatusCode.ShouldBe(HttpStatusCode.OK);
        responses[0].Body.GetProperty("id").GetInt32().ShouldBe(7);
        responses[0].Body.GetProperty("result").GetRawText().ShouldContain("HTTP test result");

        responses[1].StatusCode.ShouldBe(HttpStatusCode.OK);
        responses[1].Body.GetProperty("id").GetInt32().ShouldBe(7);
        responses[1].Body.GetProperty("result").GetRawText().ShouldNotContain("HTTP test result");
    }

    [Fact(DisplayName = "DELETE ends only the calling client's session")]
    public async Task DeleteEndsOnlyItsOwnSession()
    {
        await StartServerAsync(33342);
        using var first = new TestMcpClient(_endpoint);
        using var second = new TestMcpClient(_endpoint);
        await first.InitializeAsync();
        await second.InitializeAsync();

        (await first.DeleteSessionAsync()).ShouldBe(HttpStatusCode.NoContent);

        (await first.SendAsync(2, "tools/list")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await second.SendAsync(2, "tools/list")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "A request for an unknown session gets 404 so the client initializes again")]
    public async Task UnknownSessionReturns404()
    {
        await StartServerAsync(33343);
        using var client = new TestMcpClient(_endpoint);
        await client.InitializeAsync();

        client.SessionId = "not-a-session";
        (await client.SendAsync(2, "tools/list")).StatusCode.ShouldBe(HttpStatusCode.NotFound);

        client.SessionId = null;
        (await client.InitializeAsync()).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.SendAsync(3, "tools/list")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "A slow tool call from one client doesn't hold up another client's call")]
    public async Task SlowCallDoesNotBlockOtherClients()
    {
        var gate = new ToolGate();
        await StartServerAsync(33344, services => services.AddSingleton(gate).AddMcpTool<GatedTestTool>());
        using var first = new TestMcpClient(_endpoint);
        using var second = new TestMcpClient(_endpoint);
        await first.InitializeAsync();
        await second.InitializeAsync();

        var slowCall = first.SendAsync(4, "tools/call", new { name = "gated_test_tool" });
        await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        var fastCall = await second.SendAsync(5, "tools/call", new { name = "http_test_tool" })
            .WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        fastCall.StatusCode.ShouldBe(HttpStatusCode.OK);
        fastCall.Body.GetProperty("result").GetRawText().ShouldContain("HTTP test result");
        slowCall.IsCompleted.ShouldBeFalse();

        gate.Release.SetResult();
        var slow = await slowCall.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        slow.StatusCode.ShouldBe(HttpStatusCode.OK);
        slow.Body.GetProperty("result").GetRawText().ShouldContain("released");
    }

    private async Task StartServerAsync(int port, Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBus>(sp => new InMemoryMessageBus(
            sp,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>()));
        services.AddMcpHttpTransport();
        services.AddMcp(options =>
        {
            options.HttpPort = port;
            options.HttpPath = "/mcp";
        });
        services.AddMcpTool<HttpTestTool>();
        configure?.Invoke(services);

        _services = services.BuildServiceProvider();
        _services.UseMcp(typeof(HttpTestTool).Assembly);

        var transport = (HttpMcpTransport)_services.GetRequiredService<IMcpTransport>();
        await transport.StartAsync();
        _endpoint = transport.Endpoint!;

        var server = _services.GetRequiredService<IMcpServer>();
        _cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        _serverTask = Task.Run(() => server.RunAsync(_cts.Token));
    }

    private sealed class TestMcpClient(string endpoint) : IDisposable
    {
        private readonly HttpClient _http = new();

        public string? SessionId { get; set; }

        public async Task<(HttpStatusCode StatusCode, JsonElement Body)> InitializeAsync()
        {
            var response = await SendAsync(1, "initialize", new { protocolVersion = "2024-11-05" });
            if (response.StatusCode == HttpStatusCode.OK)
            {
                await SendNotificationAsync("notifications/initialized");
            }

            return response;
        }

        public async Task<(HttpStatusCode StatusCode, JsonElement Body)> SendAsync(int id, string method, object? parameters = null)
        {
            using var response = await PostAsync(new { jsonrpc = "2.0", id, method, @params = parameters });

            if (response.Headers.TryGetValues("Mcp-Session-Id", out var sessionIds))
            {
                SessionId = sessionIds.First();
            }

            var json = await response.Content.ReadAsStringAsync();
            var body = string.IsNullOrEmpty(json) ? default : JsonDocument.Parse(json).RootElement.Clone();
            return (response.StatusCode, body);
        }

        public async Task<HttpStatusCode> DeleteSessionAsync()
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            request.Headers.Add("Mcp-Session-Id", SessionId);
            using var response = await _http.SendAsync(request);
            return response.StatusCode;
        }

        private async Task SendNotificationAsync(string method)
        {
            using var response = await PostAsync(new { jsonrpc = "2.0", method });
            response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        }

        private async Task<HttpResponseMessage> PostAsync(object payload)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            if (SessionId != null)
            {
                request.Headers.Add("Mcp-Session-Id", SessionId);
            }

            return await _http.SendAsync(request);
        }

        public void Dispose() => _http.Dispose();
    }
}

public sealed class ToolGate
{
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

[McpTool(Name = "gated_test_tool", Description = "Waits until the test releases it")]
public class GatedTestTool(ToolGate gate) : IMcpTool
{
    public async Task<McpToolResult> ExecuteAsync(object? input, CancellationToken cancellationToken = default)
    {
        gate.Entered.SetResult();
        await gate.Release.Task.WaitAsync(cancellationToken);
        return McpToolResult.Text("released");
    }
}
