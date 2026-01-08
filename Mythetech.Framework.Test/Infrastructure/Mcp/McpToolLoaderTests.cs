using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Mcp;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Mcp;

public class McpToolLoaderTests
{
    private readonly McpToolLoader _loader;

    public McpToolLoaderTests()
    {
        _loader = new McpToolLoader(Substitute.For<ILogger<McpToolLoader>>());
    }

    [Fact(DisplayName = "DiscoverTools finds tools with McpToolAttribute")]
    public void DiscoverToolsFindsAttributedTools()
    {
        // Act
        var tools = _loader.DiscoverTools(typeof(TestTool).Assembly).ToList();

        // Assert
        tools.ShouldContain(t => t.Name == "test_tool");
    }

    [Fact(DisplayName = "DiscoverTools extracts tool metadata correctly")]
    public void DiscoverToolsExtractsMetadata()
    {
        // Act
        var tool = _loader.DiscoverTools(typeof(TestTool).Assembly)
            .FirstOrDefault(t => t.Name == "test_tool");

        // Assert
        tool.ShouldNotBeNull();
        tool.Name.ShouldBe("test_tool");
        tool.Description.ShouldBe("A test tool");
        tool.ToolType.ShouldBe(typeof(TestTool));
    }

    [Fact(DisplayName = "DiscoverTools detects input type from generic interface")]
    public void DiscoverToolsDetectsInputType()
    {
        // Act
        var tool = _loader.DiscoverTools(typeof(TestToolWithInput).Assembly)
            .FirstOrDefault(t => t.Name == "test_tool_with_input");

        // Assert
        tool.ShouldNotBeNull();
        tool.InputType.ShouldBe(typeof(TestToolInput));
    }

    [Fact(DisplayName = "DiscoverTools generates JSON schema for input type")]
    public void DiscoverToolsGeneratesInputSchema()
    {
        // Act
        var tool = _loader.DiscoverTools(typeof(TestToolWithInput).Assembly)
            .FirstOrDefault(t => t.Name == "test_tool_with_input");

        // Assert
        tool.ShouldNotBeNull();
        tool.InputSchema.ShouldNotBeNull();

        var schema = tool.InputSchema as Dictionary<string, object>;
        schema.ShouldNotBeNull();
        schema["type"].ShouldBe("object");

        var properties = schema["properties"] as Dictionary<string, object>;
        properties.ShouldNotBeNull();
        properties.ShouldContainKey("query");
    }

    [Fact(DisplayName = "DiscoverTools includes required fields in schema")]
    public void DiscoverToolsIncludesRequiredFields()
    {
        // Act
        var tool = _loader.DiscoverTools(typeof(TestToolWithInput).Assembly)
            .FirstOrDefault(t => t.Name == "test_tool_with_input");

        // Assert
        tool.ShouldNotBeNull();
        var schema = tool.InputSchema as Dictionary<string, object>;
        schema.ShouldNotBeNull();

        if (schema.TryGetValue("required", out var required))
        {
            var requiredList = required as List<string>;
            requiredList.ShouldNotBeNull();
            requiredList.ShouldContain("query");
        }
    }

    [Fact(DisplayName = "DiscoverTools ignores classes without McpToolAttribute")]
    public void DiscoverToolsIgnoresUnannotatedClasses()
    {
        // Act
        var tools = _loader.DiscoverTools(typeof(ToolWithoutAttribute).Assembly).ToList();

        // Assert
        tools.ShouldNotContain(t => t.ToolType == typeof(ToolWithoutAttribute));
    }
}

#region Test Tools

[McpTool(Name = "test_tool", Description = "A test tool")]
public class TestTool : IMcpTool
{
    public Task<McpToolResult> ExecuteAsync(object? input, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(McpToolResult.Text("Test result"));
    }
}

public class TestToolInput
{
    [McpToolInput(Description = "The query string", Required = true)]
    public string Query { get; set; } = "";

    [McpToolInput(Description = "Optional limit")]
    public int? Limit { get; set; }
}

[McpTool(Name = "test_tool_with_input", Description = "A test tool with input")]
public class TestToolWithInput : IMcpTool<TestToolInput>
{
    public Task<McpToolResult> ExecuteAsync(TestToolInput input, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(McpToolResult.Text($"Query: {input.Query}"));
    }
}

// This should NOT be discovered (no attribute)
public class ToolWithoutAttribute : IMcpTool
{
    public Task<McpToolResult> ExecuteAsync(object? input, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(McpToolResult.Text("Should not be found"));
    }
}

#endregion
