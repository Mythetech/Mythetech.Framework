using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// Base class for MCP tools that provides common functionality
/// like error handling, logging, and result building.
/// </summary>
/// <typeparam name="TInput">The input parameter type</typeparam>
public abstract class McpToolBase<TInput> : IMcpTool<TInput> where TInput : class
{
    /// <summary>
    /// Logger for the tool. Override to provide a more specific logger category.
    /// </summary>
    protected abstract ILogger Logger { get; }

    /// <summary>
    /// The name of the tool for logging purposes.
    /// Defaults to the class name, but can be overridden.
    /// </summary>
    protected virtual string ToolName => GetType().Name;

    /// <summary>
    /// Executes the tool with the provided input, with automatic error handling.
    /// </summary>
    public async Task<McpToolResult> ExecuteAsync(TInput input, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Executing {ToolName}", ToolName);

            // Validate input
            var validationResult = ValidateInput(input);
            if (!validationResult.IsValid)
            {
                Logger.LogWarning("Input validation failed for {ToolName}: {Error}", ToolName, validationResult.ErrorMessage);
                return McpToolResult.Error($"Invalid input: {validationResult.ErrorMessage}");
            }

            // Execute the tool logic
            var result = await ExecuteCoreAsync(input, cancellationToken);

            Logger.LogDebug("Completed {ToolName}", ToolName);
            return result;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("{ToolName} was cancelled", ToolName);
            return McpToolResult.Error($"{ToolName} was cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing {ToolName}", ToolName);
            return McpToolResult.Error($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Implement this method with the tool's core logic.
    /// Error handling is provided by the base class.
    /// </summary>
    /// <param name="input">The validated input</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The tool result</returns>
    protected abstract Task<McpToolResult> ExecuteCoreAsync(TInput input, CancellationToken cancellationToken);

    /// <summary>
    /// Override to provide custom input validation.
    /// Default implementation accepts all inputs.
    /// </summary>
    /// <param name="input">The input to validate</param>
    /// <returns>Validation result</returns>
    protected virtual ValidationResult ValidateInput(TInput input)
    {
        if (input == null)
        {
            return ValidationResult.Failure("Input cannot be null");
        }

        return ValidationResult.Success();
    }

    // Helper methods for building results

    /// <summary>
    /// Creates a successful text result.
    /// </summary>
    protected static McpToolResult TextResult(string text) => McpToolResult.Text(text);

    /// <summary>
    /// Creates an error result.
    /// </summary>
    protected static McpToolResult ErrorResult(string message) => McpToolResult.Error(message);

    /// <summary>
    /// Creates a formatted text result.
    /// </summary>
    protected static McpToolResult FormattedResult(string format, params object[] args)
        => McpToolResult.Text(string.Format(format, args));
}

/// <summary>
/// Result of input validation.
/// </summary>
public readonly record struct ValidationResult(bool IsValid, string? ErrorMessage = null)
{
    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new(true);

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
}

/// <summary>
/// Base class for MCP tools that don't require input.
/// </summary>
public abstract class McpToolBase : IMcpTool
{
    /// <summary>
    /// Logger for the tool.
    /// </summary>
    protected abstract ILogger Logger { get; }

    /// <summary>
    /// The name of the tool for logging purposes.
    /// </summary>
    protected virtual string ToolName => GetType().Name;

    /// <summary>
    /// Executes the tool with automatic error handling.
    /// </summary>
    public async Task<McpToolResult> ExecuteAsync(object? input, CancellationToken cancellationToken = default)
    {
        try
        {
            Logger.LogDebug("Executing {ToolName}", ToolName);
            var result = await ExecuteCoreAsync(cancellationToken);
            Logger.LogDebug("Completed {ToolName}", ToolName);
            return result;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("{ToolName} was cancelled", ToolName);
            return McpToolResult.Error($"{ToolName} was cancelled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing {ToolName}", ToolName);
            return McpToolResult.Error($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Implement this method with the tool's core logic.
    /// </summary>
    protected abstract Task<McpToolResult> ExecuteCoreAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates a successful text result.
    /// </summary>
    protected static McpToolResult TextResult(string text) => McpToolResult.Text(text);

    /// <summary>
    /// Creates an error result.
    /// </summary>
    protected static McpToolResult ErrorResult(string message) => McpToolResult.Error(message);
}
