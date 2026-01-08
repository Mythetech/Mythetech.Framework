using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.Mcp;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Mcp;

public class McpToolRegistryTests
{
    private readonly McpToolRegistry _registry;

    public McpToolRegistryTests()
    {
        _registry = new McpToolRegistry(Substitute.For<ILogger<McpToolRegistry>>());
    }

    [Fact(DisplayName = "RegisterTool adds tool to registry")]
    public void RegisterToolAddsToRegistry()
    {
        // Arrange
        var descriptor = CreateDescriptor("my_tool", "My Tool");

        // Act
        _registry.RegisterTool(descriptor);

        // Assert
        _registry.HasTool("my_tool").ShouldBeTrue();
        _registry.Count.ShouldBe(1);
    }

    [Fact(DisplayName = "GetTool returns registered tool")]
    public void GetToolReturnsRegisteredTool()
    {
        // Arrange
        var descriptor = CreateDescriptor("my_tool", "My Tool");
        _registry.RegisterTool(descriptor);

        // Act
        var result = _registry.GetTool("my_tool");

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("my_tool");
        result.Description.ShouldBe("My Tool");
    }

    [Fact(DisplayName = "GetTool returns null for unknown tool")]
    public void GetToolReturnsNullForUnknown()
    {
        // Act
        var result = _registry.GetTool("unknown_tool");

        // Assert
        result.ShouldBeNull();
    }

    [Fact(DisplayName = "GetTool is case-insensitive")]
    public void GetToolIsCaseInsensitive()
    {
        // Arrange
        var descriptor = CreateDescriptor("My_Tool", "My Tool");
        _registry.RegisterTool(descriptor);

        // Act & Assert
        _registry.GetTool("MY_TOOL").ShouldNotBeNull();
        _registry.GetTool("my_tool").ShouldNotBeNull();
        _registry.GetTool("My_Tool").ShouldNotBeNull();
    }

    [Fact(DisplayName = "GetAllTools returns all registered tools")]
    public void GetAllToolsReturnsAll()
    {
        // Arrange
        _registry.RegisterTool(CreateDescriptor("tool1", "Tool 1"));
        _registry.RegisterTool(CreateDescriptor("tool2", "Tool 2"));
        _registry.RegisterTool(CreateDescriptor("tool3", "Tool 3"));

        // Act
        var tools = _registry.GetAllTools();

        // Assert
        tools.Count.ShouldBe(3);
        tools.ShouldContain(t => t.Name == "tool1");
        tools.ShouldContain(t => t.Name == "tool2");
        tools.ShouldContain(t => t.Name == "tool3");
    }

    [Fact(DisplayName = "RegisterTool overwrites existing tool with same name")]
    public void RegisterToolOverwritesExisting()
    {
        // Arrange
        var original = CreateDescriptor("my_tool", "Original");
        var updated = CreateDescriptor("my_tool", "Updated");
        _registry.RegisterTool(original);

        // Act
        _registry.RegisterTool(updated);

        // Assert
        _registry.Count.ShouldBe(1);
        _registry.GetTool("my_tool")!.Description.ShouldBe("Updated");
    }

    private static McpToolDescriptor CreateDescriptor(string name, string description)
    {
        return new McpToolDescriptor
        {
            Name = name,
            Description = description,
            ToolType = typeof(TestTool),
            InputType = null,
            InputSchema = null
        };
    }
}
