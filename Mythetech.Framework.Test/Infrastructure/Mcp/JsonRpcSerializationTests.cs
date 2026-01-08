using System.Text.Json;
using Mythetech.Framework.Infrastructure.Mcp.Protocol.JsonRpc;
using Mythetech.Framework.Infrastructure.Mcp.Protocol.Messages;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Mcp;

public class JsonRpcSerializationTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    #region Request Serialization

    [Fact(DisplayName = "JsonRpcRequest deserializes initialize request correctly")]
    public void DeserializeInitializeRequest()
    {
        // Arrange
        var json = """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05"}}""";

        // Act
        var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, _options);

        // Assert
        request.ShouldNotBeNull();
        request.JsonRpc.ShouldBe("2.0");
        request.Id.ShouldNotBeNull();
        request.Method.ShouldBe("initialize");
        request.Params.ShouldNotBeNull();
        request.IsNotification.ShouldBeFalse();
    }

    [Fact(DisplayName = "JsonRpcRequest detects notifications (no id)")]
    public void DetectsNotifications()
    {
        // Arrange
        var json = """{"jsonrpc":"2.0","method":"initialized"}""";

        // Act
        var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, _options);

        // Assert
        request.ShouldNotBeNull();
        request.Id.ShouldBeNull();
        request.IsNotification.ShouldBeTrue();
    }

    [Fact(DisplayName = "JsonRpcRequest deserializes tools/call request with arguments")]
    public void DeserializeToolsCallRequest()
    {
        // Arrange
        var json = """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"get_info","arguments":{"query":"test"}}}""";

        // Act
        var request = JsonSerializer.Deserialize<JsonRpcRequest>(json, _options);

        // Assert
        request.ShouldNotBeNull();
        request.Method.ShouldBe("tools/call");

        var callParams = request.Params?.Deserialize<McpToolCallParams>(_options);
        callParams.ShouldNotBeNull();
        callParams.Name.ShouldBe("get_info");
        callParams.Arguments.ShouldNotBeNull();
    }

    #endregion

    #region Response Serialization

    [Fact(DisplayName = "JsonRpcResponse.Success serializes correctly")]
    public void SerializeSuccessResponse()
    {
        // Arrange
        var response = JsonRpcResponse.Success(1, new { data = "test" });

        // Use options that ignore null values (like the transport does)
        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Act
        var json = JsonSerializer.Serialize(response, serializeOptions);

        // Assert
        json.ShouldContain("\"jsonrpc\":\"2.0\"");
        json.ShouldContain("\"id\":1");
        json.ShouldContain("\"result\"");
        json.ShouldNotContain("\"error\"");
    }

    [Fact(DisplayName = "JsonRpcResponse.Failure serializes error correctly")]
    public void SerializeErrorResponse()
    {
        // Arrange
        var response = JsonRpcResponse.Failure(1, JsonRpcError.MethodNotFound, "Unknown method");

        // Act
        var json = JsonSerializer.Serialize(response, _options);

        // Assert
        json.ShouldContain("\"jsonrpc\":\"2.0\"");
        json.ShouldContain("\"id\":1");
        json.ShouldContain("\"error\"");
        json.ShouldContain("-32601"); // MethodNotFound code
        json.ShouldContain("Unknown method");
    }

    #endregion

    #region MCP Message Types

    [Fact(DisplayName = "McpInitializeResult serializes correctly")]
    public void SerializeInitializeResult()
    {
        // Arrange
        var result = new McpInitializeResult
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new McpServerCapabilities
            {
                Tools = new McpToolsCapability { ListChanged = false }
            },
            ServerInfo = new McpServerInfo { Name = "TestServer", Version = "1.0.0" }
        };

        // Act
        var json = JsonSerializer.Serialize(result, _options);

        // Assert
        json.ShouldContain("\"protocolVersion\":\"2024-11-05\"");
        json.ShouldContain("\"capabilities\"");
        json.ShouldContain("\"tools\"");
        json.ShouldContain("\"serverInfo\"");
    }

    [Fact(DisplayName = "McpToolsListResult serializes tools array")]
    public void SerializeToolsListResult()
    {
        // Arrange
        var result = new McpToolsListResult
        {
            Tools = new[]
            {
                new McpToolDefinition { Name = "get_info", Description = "Gets info" },
                new McpToolDefinition { Name = "do_action", Description = "Does action", InputSchema = new { type = "object" } }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(result, _options);

        // Assert
        json.ShouldContain("\"tools\"");
        json.ShouldContain("\"get_info\"");
        json.ShouldContain("\"do_action\"");
    }

    #endregion
}
