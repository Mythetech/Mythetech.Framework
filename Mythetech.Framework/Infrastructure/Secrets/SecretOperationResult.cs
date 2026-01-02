namespace Mythetech.Framework.Infrastructure.Secrets;

/// <summary>
/// Represents the result of a secret operation, distinguishing between failures and successful empty results
/// </summary>
public class SecretOperationResult
{
    /// <summary>
    /// Whether the operation completed successfully
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Category of error if the operation failed
    /// </summary>
    public SecretOperationErrorKind? ErrorKind { get; init; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static SecretOperationResult Ok() => new() { Success = true };

    /// <summary>
    /// Creates a failed result with an error message and kind
    /// </summary>
    public static SecretOperationResult Fail(string message, SecretOperationErrorKind kind) =>
        new() { Success = false, ErrorMessage = message, ErrorKind = kind };
}

/// <summary>
/// Represents the result of a secret operation that returns a value
/// </summary>
/// <typeparam name="T">The type of value returned on success</typeparam>
public class SecretOperationResult<T> : SecretOperationResult
{
    /// <summary>
    /// The value returned if the operation was successful
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    public static SecretOperationResult<T> Ok(T value) => new() { Success = true, Value = value };

    /// <summary>
    /// Creates a failed result with an error message and kind
    /// </summary>
    public static new SecretOperationResult<T> Fail(string message, SecretOperationErrorKind kind) =>
        new() { Success = false, ErrorMessage = message, ErrorKind = kind };
}

/// <summary>
/// Categories of errors that can occur during secret operations
/// </summary>
public enum SecretOperationErrorKind
{
    /// <summary>
    /// The requested secret was not found
    /// </summary>
    NotFound,

    /// <summary>
    /// Access to the secret was denied (e.g., keychain locked, not authenticated)
    /// </summary>
    AccessDenied,

    /// <summary>
    /// Could not connect to the secret manager (e.g., CLI not installed, library not found)
    /// </summary>
    ConnectionFailed,

    /// <summary>
    /// The operation is not supported by this secret manager
    /// </summary>
    NotSupported,

    /// <summary>
    /// The provided key was invalid or malformed
    /// </summary>
    InvalidKey,

    /// <summary>
    /// An unknown error occurred
    /// </summary>
    Unknown
}

