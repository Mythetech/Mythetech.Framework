namespace Mythetech.Framework.Infrastructure.Secrets;

/// <summary>
/// Core interface for secret manager implementations.
/// Provides basic secret retrieval and connection testing.
/// </summary>
public interface ISecretManager
{
    /// <summary>
    /// Display name for this secret manager (e.g., "1Password CLI", "macOS Keychain")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Get a specific secret by key
    /// </summary>
    Task<SecretOperationResult<Secret>> GetSecretAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Test if the secret manager is available/configured
    /// </summary>
    Task<SecretOperationResult> TestConnectionAsync(CancellationToken cancellationToken = default);
}

