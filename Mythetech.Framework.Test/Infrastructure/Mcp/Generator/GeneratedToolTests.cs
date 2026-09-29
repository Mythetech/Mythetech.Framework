using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Mcp;
using Mythetech.Framework.Infrastructure.Mcp.Messages;
using Mythetech.Framework.Test.Infrastructure.Mcp.Generator.Generated;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Mcp.Generator;

/// <summary>
/// Exercises tools the generator emits for the messages below, which it sees because this
/// project references Mythetech.Framework.AI.Generator as an analyzer.
/// </summary>
public class GeneratedToolTests : IDisposable
{
    private static readonly Guid WorkspaceId = Guid.Parse("7f0c4a52-9d0e-4a57-8d3c-2f1a6b9e0c11");

    private readonly ServiceProvider _services;
    private readonly IMessageBus _bus;
    private readonly McpToolRegistry _registry;

    public GeneratedToolTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMessageBus>(sp => new InMemoryMessageBus(
            sp,
            Substitute.For<ILogger<InMemoryMessageBus>>(),
            Array.Empty<IMessagePipe>(),
            Array.Empty<IConsumerFilter>()));
        services.AddMcp();
        services.AddTransient<AddTestReposHandler>();
        services.AddTransient<GetTestEchoHandler>();
        services.AddTransient<GetLegacyGreetingHandler>();
        Mythetech.Framework.Test.Generated.McpToolRegistration.AddGeneratedMcpTools(services);

        _services = services.BuildServiceProvider();
        _services.UseMcp(typeof(GeneratedToolTests).Assembly);

        _bus = _services.GetRequiredService<IMessageBus>();
        _bus.RegisterQueryHandler<AddTestRepos, ToolResult<TestWorkspaceRepos>, AddTestReposHandler>();
        _bus.RegisterQueryHandler<GetTestEcho, TestEcho, GetTestEchoHandler>();
#pragma warning disable CS0618
        _bus.RegisterQueryHandler<GetLegacyGreeting, string, GetLegacyGreetingHandler>();
#pragma warning restore CS0618

        _registry = _services.GetRequiredService<McpToolRegistry>();
    }

    public void Dispose() => _services.Dispose();

    [Fact(DisplayName = "Generated input schema describes Guid, array, enum and DateTimeOffset parameters")]
    public void SchemaDescribesParameterTypes()
    {
        var properties = GetSchemaProperties("add_test_repos");

        properties["workspaceId"]["type"].ShouldBe("string");
        properties["workspaceId"]["format"].ShouldBe("uuid");
        properties["workspaceId"]["description"].ShouldBe("The workspace to add to.");

        properties["paths"]["type"].ShouldBe("array");
        ((Dictionary<string, object>)properties["paths"]["items"])["type"].ShouldBe("string");

        properties["kind"]["type"].ShouldBe("string");
        properties["kind"]["enum"].ShouldBe(new[] { "Library", "App" });

        properties["since"]["type"].ShouldBe("string");
        properties["since"]["format"].ShouldBe("date-time");

        properties["topK"]["type"].ShouldBe("integer");
    }

    [Fact(DisplayName = "Parameters with defaults are not required")]
    public void DefaultedParametersAreOptional()
    {
        var schema = (Dictionary<string, object>)_registry.GetTool("add_test_repos")!.InputSchema!;

        ((List<string>)schema["required"]).ShouldBe(new[] { "workspaceId", "paths" }, ignoreOrder: true);
    }

    [Fact(DisplayName = "Generated input properties start at the declared constructor defaults")]
    public void InputPropertiesUseDeclaredDefaults()
    {
        var repos = new AddTestReposMcpInput();
        repos.Kind.ShouldBe(RepoKind.App);
        repos.TopK.ShouldBe(10);
        repos.Since.ShouldBeNull();

        var echo = new GetTestEchoMcpInput();
        echo.Text.ShouldBe("say \"hi\"");
        echo.Scale.ShouldBe(1.5);
        echo.Ratio.ShouldBe(0.25f);
        echo.Price.ShouldBe(9.99m);
        echo.Loud.ShouldBeTrue();
        echo.Mark.ShouldBe('\'');
        echo.Big.ShouldBe(5_000_000_000L);
        echo.Suffix.ShouldBeNull();
    }

    [Fact(DisplayName = "A request tool returns its response as JSON, filling omitted parameters with defaults")]
    public async Task RequestReturnsJson()
    {
        var result = await CallAsync("add_test_repos",
            $$"""{"workspaceId":"{{WorkspaceId}}","paths":["/code/api"],"kind":"Library"}""");

        result.IsError.ShouldBeFalse();
        var json = JsonDocument.Parse(TextOf(result)).RootElement;
        json.GetProperty("workspaceId").GetGuid().ShouldBe(WorkspaceId);
        json.GetProperty("paths")[0].GetString().ShouldBe("/code/api");
        json.GetProperty("kind").GetString().ShouldBe("Library");
        json.GetProperty("topK").GetInt32().ShouldBe(10);
    }

    [Fact(DisplayName = "A ToolResult failure becomes an MCP error carrying its code and message")]
    public async Task FailureBecomesMcpError()
    {
        var result = await CallAsync("add_test_repos",
            $$"""{"workspaceId":"{{WorkspaceId}}","paths":["/missing"]}""");

        result.IsError.ShouldBeTrue();
        TextOf(result).ShouldBe("path_not_found: /missing does not exist");
    }

    [Fact(DisplayName = "C# callers of the same message get the ToolResult back")]
    public async Task CSharpCallersGetToolResult()
    {
        var failure = await _bus.SendAsync<AddTestRepos, ToolResult<TestWorkspaceRepos>>(
            new AddTestRepos(WorkspaceId, ["/missing"]));

        failure.IsSuccess.ShouldBeFalse();
        failure.Error.ShouldBe(new ToolError("path_not_found", "/missing does not exist"));

        var success = await _bus.SendAsync<AddTestRepos, ToolResult<TestWorkspaceRepos>>(
            new AddTestRepos(WorkspaceId, ["/code/api"]));

        success.IsSuccess.ShouldBeTrue();
        success.Value!.Paths.ShouldBe(new[] { "/code/api" });
    }

    [Fact(DisplayName = "A tool called without arguments uses every declared default")]
    public async Task CallWithoutArgumentsUsesDefaults()
    {
        var result = await CallAsync("get_test_echo", arguments: null);

        result.IsError.ShouldBeFalse();
        var json = JsonDocument.Parse(TextOf(result)).RootElement;
        json.GetProperty("text").GetString().ShouldBe("say \"hi\"");
        json.GetProperty("scale").GetDouble().ShouldBe(1.5);
    }

    [Fact(DisplayName = "The obsolete [ToolQuery] still generates a tool, and a string response is returned as-is")]
    public async Task LegacyToolQueryStillGenerates()
    {
        var result = await CallAsync("get_legacy_greeting", """{"name":"Ada"}""");

        result.IsError.ShouldBeFalse();
        TextOf(result).ShouldBe("Hello, Ada");
    }

    private Dictionary<string, Dictionary<string, object>> GetSchemaProperties(string toolName)
    {
        var schema = (Dictionary<string, object>)_registry.GetTool(toolName)!.InputSchema!;
        return ((Dictionary<string, object>)schema["properties"])
            .ToDictionary(p => p.Key, p => (Dictionary<string, object>)p.Value);
    }

    private async Task<McpToolResult> CallAsync(string toolName, string? arguments)
    {
        var response = await _bus.SendAsync<McpToolCallMessage, McpToolCallResponse>(new McpToolCallMessage
        {
            ToolName = toolName,
            Arguments = arguments is null ? null : JsonDocument.Parse(arguments).RootElement
        });

        return response.Result;
    }

    private static string TextOf(McpToolResult result) => ((McpTextContent)result.Content.Single()).Text;
}

public enum RepoKind
{
    Library,
    App
}

public record TestWorkspaceRepos(Guid WorkspaceId, IReadOnlyList<string> Paths, RepoKind Kind, int TopK);

/// <summary>Adds git repos to a workspace and returns the workspace's repos.</summary>
/// <param name="WorkspaceId">The workspace to add to.</param>
/// <param name="Paths">Absolute paths of the repo folders.</param>
/// <param name="Kind">What the repos hold.</param>
/// <param name="TopK">How many repos to return.</param>
/// <param name="Since">Only repos changed after this time.</param>
[ToolRequest(ResponseType = typeof(ToolResult<TestWorkspaceRepos>))]
public record AddTestRepos(Guid WorkspaceId, string[] Paths, RepoKind Kind = RepoKind.App, int TopK = 10, DateTimeOffset? Since = null);

public class AddTestReposHandler : IQueryHandler<AddTestRepos, ToolResult<TestWorkspaceRepos>>
{
    public Task<ToolResult<TestWorkspaceRepos>> Handle(AddTestRepos message)
    {
        var missing = message.Paths.FirstOrDefault(p => p == "/missing");
        ToolResult<TestWorkspaceRepos> result = missing is not null
            ? new ToolError("path_not_found", $"{missing} does not exist")
            : new TestWorkspaceRepos(message.WorkspaceId, message.Paths, message.Kind, message.TopK);

        return Task.FromResult(result);
    }
}

public record TestEcho(string Text, double Scale);

/// <summary>Echoes its arguments.</summary>
[ToolRequest(ResponseType = typeof(TestEcho))]
public record GetTestEcho(
    string Text = "say \"hi\"",
    double Scale = 1.5,
    float Ratio = 0.25f,
    decimal Price = 9.99m,
    bool Loud = true,
    char Mark = '\'',
    long Big = 5_000_000_000L,
    string? Suffix = null);

public class GetTestEchoHandler : IQueryHandler<GetTestEcho, TestEcho>
{
    public Task<TestEcho> Handle(GetTestEcho message) => Task.FromResult(new TestEcho(message.Text, message.Scale));
}

#pragma warning disable CS0618
/// <summary>Greets someone.</summary>
[ToolQuery(ResponseType = typeof(string))]
public record GetLegacyGreeting(string Name);

public class GetLegacyGreetingHandler : IQueryHandler<GetLegacyGreeting, string>
{
    public Task<string> Handle(GetLegacyGreeting message) => Task.FromResult($"Hello, {message.Name}");
}
#pragma warning restore CS0618
