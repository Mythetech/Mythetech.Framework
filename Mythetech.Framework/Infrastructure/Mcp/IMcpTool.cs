namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Base interface for MCP tools. Tools handle specific operations
/// that can be invoked by MCP clients (like Claude Code).
/// </summary>
public interface IMcpTool
{
    /// <summary>
    /// Execute the tool with the provided input.
    /// </summary>
    /// <param name="input">JSON-deserialized input parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tool execution result</returns>
    Task<McpToolResult> ExecuteAsync(object? input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Strongly-typed tool interface for type-safe input handling.
/// </summary>
/// <typeparam name="TInput">The input parameter type</typeparam>
public interface IMcpTool<TInput> : IMcpTool where TInput : class
{
    /// <summary>
    /// Execute the tool with strongly-typed input.
    /// </summary>
    Task<McpToolResult> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Default implementation bridges untyped to typed execution.
    /// </summary>
    Task<McpToolResult> IMcpTool.ExecuteAsync(object? input, CancellationToken cancellationToken)
        => ExecuteAsync((TInput)input!, cancellationToken);
}
