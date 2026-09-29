namespace Mythetech.Framework.Infrastructure.Mcp;

/// <summary>
/// The outcome of a request handler: a value on success, or a <see cref="ToolError"/> for an expected failure.
/// </summary>
/// <remarks>
/// Expected failures (a missing file, an item that already exists) are values rather than exceptions,
/// so C# callers check <see cref="IsSuccess"/> instead of catching, and MCP clients get an error result
/// with the failure's code and message. Unexpected failures should still throw.
/// </remarks>
/// <typeparam name="T">The success value type</typeparam>
public sealed class ToolResult<T>
{
    private ToolResult(T? value, ToolError? error)
    {
        Value = value;
        Error = error;
    }

    /// <summary>
    /// True when the request succeeded and <see cref="Value"/> holds its result.
    /// </summary>
    public bool IsSuccess => Error is null;

    /// <summary>
    /// The success value, or default when the request failed.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// The failure, or null when the request succeeded.
    /// </summary>
    public ToolError? Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ToolResult<T> Success(T value) => new(value, null);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="code">A short, stable identifier for the failure, such as <c>not_a_git_repo</c></param>
    /// <param name="message">A readable explanation of what went wrong</param>
    public static ToolResult<T> Failure(string code, string message) => new(default, new ToolError(code, message));

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static ToolResult<T> Failure(ToolError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new(default, error);
    }

    /// <summary>
    /// Converts a value to a successful result.
    /// </summary>
    public static implicit operator ToolResult<T>(T value) => Success(value);

    /// <summary>
    /// Converts an error to a failed result.
    /// </summary>
    public static implicit operator ToolResult<T>(ToolError error) => Failure(error);
}

/// <summary>
/// An expected failure returned in a <see cref="ToolResult{T}"/>.
/// </summary>
/// <param name="Code">A short, stable identifier for the failure, such as <c>not_a_git_repo</c></param>
/// <param name="Message">A readable explanation of what went wrong</param>
public sealed record ToolError(string Code, string Message)
{
    /// <inheritdoc />
    public override string ToString() => $"{Code}: {Message}";
}
