using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using System.Text;
using Mythetech.Framework.Infrastructure.Mcp.Protocol.JsonRpc;
using Mythetech.Framework.Infrastructure.Mcp.Server;
using Mythetech.Framework.Infrastructure.Mcp.Transport;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Mcp;

[UnsupportedOSPlatform("browser")]
public sealed class HttpMcpTransportTokenTests : IAsyncDisposable
{
    private const string PingBody = """{"jsonrpc":"2.0","id":1,"method":"ping"}""";

    private readonly string _directory = Directory.CreateTempSubdirectory("mcp-token-").FullName;
    private readonly HttpClient _httpClient = new();
    private HttpMcpTransport? _transport;

    public async ValueTask DisposeAsync()
    {
        _httpClient.Dispose();
        if (_transport != null)
        {
            await _transport.DisposeAsync();
        }
        Directory.Delete(_directory, recursive: true);
    }

    [Fact(DisplayName = "Request_without_a_token_is_refused_with_401_and_a_bearer_challenge")]
    public async Task Request_without_a_token_is_refused_with_401_and_a_bearer_challenge()
    {
        await StartAsync(33360, withToken: true);

        using var response = await PostAsync(authorization: null);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ToString().ShouldBe("Bearer");
    }

    [Fact(DisplayName = "Request_with_the_wrong_token_is_refused")]
    public async Task Request_with_the_wrong_token_is_refused()
    {
        await StartAsync(33361, withToken: true);

        using var response = await PostAsync(new AuthenticationHeaderValue("Bearer", "not-the-token"));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Request_with_the_token_reaches_the_server")]
    public async Task Request_with_the_token_reaches_the_server()
    {
        await StartAsync(33362, withToken: true);
        _transport!.BearerToken.ShouldNotBeNullOrEmpty();

        using var response = await PostAndAnswerAsync(new AuthenticationHeaderValue("Bearer", _transport.BearerToken));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact(DisplayName = "Preflight_is_answered_without_a_token_and_allows_the_Authorization_header")]
    public async Task Preflight_is_answered_without_a_token_and_allows_the_Authorization_header()
    {
        await StartAsync(33363, withToken: true);

        using var request = new HttpRequestMessage(HttpMethod.Options, _transport!.Endpoint);
        using var response = await _httpClient.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers")).ShouldContain("Authorization");
    }

    [Fact(DisplayName = "Without_a_token_file_requests_are_accepted_as_before")]
    public async Task Without_a_token_file_requests_are_accepted_as_before()
    {
        await StartAsync(33364, withToken: false);
        _transport!.BearerToken.ShouldBeNull();

        using var response = await PostAndAnswerAsync(authorization: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private async Task StartAsync(int port, bool withToken)
    {
        _transport = new HttpMcpTransport(new McpServerOptions
        {
            HttpPort = port,
            HttpPath = "/mcp",
            HttpTokenFilePath = withToken ? Path.Combine(_directory, "http-token") : null,
        });
        await _transport.StartAsync(TestContext.Current.CancellationToken);
    }

    private Task<HttpResponseMessage> PostAsync(AuthenticationHeaderValue? authorization)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _transport!.Endpoint)
        {
            Content = new StringContent(PingBody, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = authorization;
        return _httpClient.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task<HttpResponseMessage> PostAndAnswerAsync(AuthenticationHeaderValue? authorization)
    {
        var received = _transport!.ReadMessageAsync(TestContext.Current.CancellationToken);
        var responseTask = PostAsync(authorization);
        var request = await received.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await _transport.WriteMessageAsync(JsonRpcResponse.Success(request!.Id, new { }), TestContext.Current.CancellationToken);
        return await responseTask.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
    }
}
