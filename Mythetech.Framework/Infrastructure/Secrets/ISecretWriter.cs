namespace Mythetech.Framework.Infrastructure.Secrets;

/// <summary>
/// Interface for secret managers that support writing secrets.
/// Primarily used by native OS keychain implementations that can store secrets locally.
/// </summary>
public interface ISecretWriter
{
    /// <summary>
    /// Store a secret with the given key and value
    /// </summary>
    Task<SecretOperationResult> SetSecretAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a secret by key
    /// </summary>
    Task<SecretOperationResult> DeleteSecretAsync(string key, CancellationToken cancellationToken = default);
}
