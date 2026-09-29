# MCP Tools

Tools are the core building blocks of MCP. Each tool represents a discrete operation that can be invoked by an AI assistant.

## Creating a Tool

### Basic Tool (No Input)

```csharp
[McpTool(Name = "get_time", Description = "Returns the current server time")]
public class GetTimeTool : IMcpTool
{
    public Task<McpToolResult> ExecuteAsync(object? input, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(McpToolResult.Text(DateTime.UtcNow.ToString("O")));
    }
}
```

### Typed Input Tool

For tools that accept parameters, implement `IMcpTool<TInput>`:

```csharp
[McpTool(Name = "calculate", Description = "Performs arithmetic calculations")]
public class CalculateTool : IMcpTool<CalculateInput>
{
    public Task<McpToolResult> ExecuteAsync(CalculateInput input, CancellationToken cancellationToken = default)
    {
        var result = input.Operation switch
        {
            "add" => input.A + input.B,
            "subtract" => input.A - input.B,
            "multiply" => input.A * input.B,
            "divide" => input.B != 0 ? input.A / input.B : throw new DivideByZeroException(),
            _ => throw new ArgumentException($"Unknown operation: {input.Operation}")
        };

        return Task.FromResult(McpToolResult.Text($"Result: {result}"));
    }
}

public class CalculateInput
{
    [McpToolInput(Description = "First operand", Required = true)]
    public double A { get; set; }

    [McpToolInput(Description = "Second operand", Required = true)]
    public double B { get; set; }

    [McpToolInput(Description = "Operation: add, subtract, multiply, divide", Required = true)]
    public string Operation { get; set; } = "add";
}
```

## Generated Tools

With the `Mythetech.Framework.AI.Generator` package, a message bus message becomes an MCP tool, so C# code and the AI client share one set of messages. The generator reads the tool's description from the message's `///` summary and each parameter's description from its `<param>` doc. It takes parameters from the primary constructor: a parameter is required unless it is nullable or has a default, and an omitted parameter gets its declared default.

### Requests

`[ToolRequest]` sends the message with `IMessageBus.SendAsync` and returns the response to the client as JSON text, or as-is for a string. Use it for any tool that returns something, whether it reads or changes state. `ResponseType` is required.

```csharp
/// <summary>Adds git repos to a workspace and returns the workspace's repos.</summary>
/// <param name="WorkspaceId">The workspace to add to, as given in the planning prompt.</param>
/// <param name="Paths">Absolute paths of the repo folders.</param>
[ToolRequest(ResponseType = typeof(ToolResult<WorkspaceRepos>))]
public record AddReposToWorkspace(Guid WorkspaceId, string[] Paths);
```

### Expected failures

When `ResponseType` is a `ToolResult<T>`, the handler returns either a value or a `ToolError` with a code and a message. A success goes to the client as JSON; a failure becomes an MCP error result (`isError: true`) whose text is `code: message`. C# callers of the same message get the `ToolResult<T>` back and check `IsSuccess`, with no catch blocks. Throw only for unexpected failures, which `McpToolCallHandler` reports as a tool error.

```csharp
public class AddReposHandler : IQueryHandler<AddReposToWorkspace, ToolResult<WorkspaceRepos>>
{
    public async Task<ToolResult<WorkspaceRepos>> Handle(AddReposToWorkspace message)
    {
        var workspace = await _workspaces.FindAsync(message.WorkspaceId);
        if (workspace is null)
            return new ToolError("workspace_not_found", $"No workspace has the id {message.WorkspaceId}");

        // ... add the repos
        return new WorkspaceRepos(workspace.Id, workspace.RepoPaths);
    }
}
```

### Commands

`[ToolCommand]` publishes the message with `IMessageBus.PublishAsync` and tells the client it ran. It returns nothing, so use `[ToolRequest]` when the client needs to know what happened.

### Registering generated tools

`services.AddMcpTools(assembly)` registers generated tools like any other. The generator also emits `AddGeneratedMcpTools()` in the `<AssemblyName>.Generated` namespace for each assembly that declares tools.

`[ToolQuery]` is the obsolete name for `[ToolRequest]`. It still works for this release.

## Calling Tools Directly

`McpToolInvoker` calls a registered tool by name, the same way the MCP server does, but without a transport or the message bus. Use it to offer the app's tools through something other than MCP, such as a chat client:

```csharp
var result = await invoker.InvokeAsync("add_repos_to_workspace", argumentsJson, cancellationToken);
```

Unknown and disabled tools, and exceptions a tool throws, come back as a result with `IsError` set rather than as exceptions. The tools and their input schemas come from `McpToolRegistry.GetEnabledTools()`.

## Tool Attributes

### McpToolAttribute

Required on all tool classes:

```csharp
[McpTool(Name = "tool_name", Description = "What this tool does")]
```

- `Name` - Unique identifier (snake_case recommended)
- `Description` - Human-readable description shown to AI

### McpToolInputAttribute

Optional on input properties:

```csharp
[McpToolInput(Description = "Parameter description", Required = true)]
public string MyParam { get; set; }
```

- `Description` - Explains the parameter to the AI
- `Required` - Whether the parameter must be provided

## Tool Results

### Success Results

```csharp
// Text content
McpToolResult.Text("Operation completed successfully");

// Multiple content items
McpToolResult.FromContent(
    new McpTextContent("First result"),
    new McpTextContent("Second result")
);
```

### Error Results

```csharp
McpToolResult.Error("Something went wrong: invalid input");
```

## Dependency Injection

Tools are resolved from DI, so you can inject services:

```csharp
[McpTool(Name = "get_user", Description = "Gets user information")]
public class GetUserTool : IMcpTool<GetUserInput>
{
    private readonly IUserService _userService;
    private readonly ILogger<GetUserTool> _logger;

    public GetUserTool(IUserService userService, ILogger<GetUserTool> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<McpToolResult> ExecuteAsync(GetUserInput input, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting user {UserId}", input.UserId);

        var user = await _userService.GetByIdAsync(input.UserId, cancellationToken);
        if (user is null)
            return McpToolResult.Error($"User {input.UserId} not found");

        return McpToolResult.Text($"User: {user.Name} ({user.Email})");
    }
}
```

## Registering Tools

### Auto-discovery

Register all tools from an assembly:

```csharp
// From calling assembly
builder.Services.AddMcpTools();

// From specific assembly
builder.Services.AddMcpTools(typeof(MyTool).Assembly);
```

### Manual Registration

Register a specific tool:

```csharp
builder.Services.AddMcpTool<MyCustomTool>();
```

## Built-in Tools

The framework includes one built-in tool:

| Tool | Description |
|------|-------------|
| `get_app_info` | Returns application name, version, runtime info |

## Input Type Mapping

Property types are mapped to JSON Schema types:

| C# Type | JSON Schema |
|---------|-------------|
| `string`, `char` | `string` |
| `Guid` | `string`, format `uuid` |
| `DateTime`, `DateTimeOffset` | `string`, format `date-time` |
| `DateOnly`, `TimeOnly`, `Uri` | `string`, format `date`, `time`, `uri` |
| Enums | `string` with an `enum` list of the names |
| Integer types | `integer` |
| `float`, `double`, `decimal` | `number` |
| `bool` | `boolean` |
| Arrays, lists, `IEnumerable<T>` | `array`, with `items` describing the element type |
| Dictionaries and other types | `object` |

Nullable types map like their underlying type. Arguments are read case-insensitively, and enums accept their names.
