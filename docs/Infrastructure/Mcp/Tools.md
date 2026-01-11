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

| C# Type | JSON Schema Type |
|---------|------------------|
| `string` | `string` |
| `int`, `long`, `short`, `byte` | `integer` |
| `float`, `double`, `decimal` | `number` |
| `bool` | `boolean` |
| Arrays, `IEnumerable<T>` | `array` |
| Other | `object` |
