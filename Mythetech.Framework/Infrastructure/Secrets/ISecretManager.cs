namespace Mythetech.Framework.Infrastructure.Secrets;

/// <summary>
/// Interface for secret manager implementations
/// </summary>
public interface ISecretManager
{
    /// <summary>
    /// List all available secrets
    /// </summary>
    Task<IEnumerable<Secret>> ListSecretsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a specific secret by key
    /// </summary>
    Task<Secret?> GetSecretAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Test if the secret manager is available/configured
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Search secrets by name/key/tags
    /// </summary>
    Task<IEnumerable<Secret>> SearchSecretsAsync(string searchTerm, CancellationToken cancellationToken = default);
}

