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

public class McpServerTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact(DisplayName = "Server responds to initialize request")]
    public async Task ServerRespondsToInitialize()
    {
        // Arrange
        var (inputStream, outputStream, server) = CreateTestServer();

        var request = """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05"}}""";
        WriteToInputStream(inputStream, request);

        // Act - Run server with cancellation
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        try { await server.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert
        var response = ReadResponse(outputStream);
        response.ShouldNotBeNull();
        response.Error.ShouldBeNull();

        var resultJson = JsonSerializer.Serialize(response.Result, _jsonOptions);
        resultJson.ShouldContain("protocolVersion");
        resultJson.ShouldContain("capabilities");
        resultJson.ShouldContain("serverInfo");
    }

    [Fact(DisplayName = "Server responds to tools/list with registered tools")]
    public async Task ServerRespondsToToolsListWithTools()
    {
        // Arrange
        var registry = new McpToolRegistry(Substitute.For<ILogger<McpToolRegistry>>());
        registry.RegisterTool(new McpToolDescriptor
        {
            Name = "my_test_tool",
            Description = "A test tool",
            ToolType = typeof(ServerTestTool),
            InputSchema = new Dictionary<string, object> { ["type"] = "object" }
        });

        var (inputStream, outputStream, server) = CreateTestServer(registry);

        var request = """{"jsonrpc":"2.0","id":1,"method":"tools/list"}""";
        WriteToInputStream(inputStream, request);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        try { await server.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert
        var response = ReadResponse(outputStream);
        response.ShouldNotBeNull();
        response.Error.ShouldBeNull();

        var resultJson = JsonSerializer.Serialize(response.Result, _jsonOptions);
        resultJson.ShouldContain("tools");
        resultJson.ShouldContain("my_test_tool");
    }

    [Fact(DisplayName = "Server responds to ping")]
    public async Task ServerRespondsToPing()
    {
        // Arrange
        var (inputStream, outputStream, server) = CreateTestServer();

        var request = """{"jsonrpc":"2.0","id":1,"method":"ping"}""";
        WriteToInputStream(inputStream, request);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        try { await server.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert
        var response = ReadResponse(outputStream);
        response.ShouldNotBeNull();
        response.Error.ShouldBeNull();
    }

    [Fact(DisplayName = "Server returns MethodNotFound for unknown methods")]
    public async Task ServerReturnsMethodNotFound()
    {
        // Arrange
        var (inputStream, outputStream, server) = CreateTestServer();

        var request = """{"jsonrpc":"2.0","id":1,"method":"unknown/method"}""";
        WriteToInputStream(inputStream, request);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        try { await server.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert
        var response = ReadResponse(outputStream);
        response.ShouldNotBeNull();
        response.Error.ShouldNotBeNull();
        response.Error.Code.ShouldBe(JsonRpcError.MethodNotFound);
    }

    [Fact(DisplayName = "Server does not respond to notifications")]
    public async Task ServerIgnoresNotifications()
    {
        // Arrange
        var (inputStream, outputStream, server) = CreateTestServer();

        // Notification has no id
        var notification = """{"jsonrpc":"2.0","method":"initialized"}""";
        WriteToInputStream(inputStream, notification);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        try { await server.RunAsync(cts.Token); } catch (OperationCanceledException) { }

        // Assert - output should be empty (no response to notifications)
        outputStream.Position = 0;
        using var reader = new StreamReader(outputStream, leaveOpen: true);
        var output = await reader.ReadToEndAsync();
        output.Trim().ShouldBeEmpty();
    }

    private (MemoryStream inputStream, MemoryStream outputStream, McpServer server) CreateTestServer(McpToolRegistry? registry = null)
    {
        var inputStream = new MemoryStream();
        var outputStream = new MemoryStream();

        var transport = new StdioMcpTransport(inputStream, outputStream);
        registry ??= new McpToolRegistry(Substitute.For<ILogger<McpToolRegistry>>());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBus>(sp => new InMemoryMessageBus(
            sp,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>()));
        var serviceProvider = services.BuildServiceProvider();

        var options = Options.Create(new McpServerOptions
        {
            ServerName = "TestServer",
            ServerVersion = "1.0.0"
        });

        var server = new McpServer(
            transport,
            serviceProvider.GetRequiredService<IMessageBus>(),
            registry,
            options,
            Substitute.For<ILogger<McpServer>>());

        return (inputStream, outputStream, server);
    }

    private void WriteToInputStream(MemoryStream stream, string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json + "\n");
        stream.Write(bytes);
        stream.Position = 0;
    }

    private JsonRpcResponse? ReadResponse(MemoryStream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, leaveOpen: true);
        var line = reader.ReadLine();
        if (string.IsNullOrEmpty(line)) return null;
        return JsonSerializer.Deserialize<JsonRpcResponse>(line, _jsonOptions);
    }
}

[McpTool(Name = "server_test_tool", Description = "Test tool for server tests")]
public class ServerTestTool : IMcpTool
{
    public Task<McpToolResult> ExecuteAsync(object? input, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(McpToolResult.Text("Server test result"));
    }
}
