namespace Mythetech.Framework.Infrastructure.Secrets;

/// <summary>
/// Interface for secret managers that support listing and searching secrets.
/// Not all secret managers support this capability (e.g., native OS keychains may not support enumeration).
/// </summary>
public interface ISecretSearcher
{
    /// <summary>
    /// List all available secrets
    /// </summary>
    Task<SecretOperationResult<IEnumerable<Secret>>> ListSecretsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Search secrets by name/key/tags
    /// </summary>
    Task<SecretOperationResult<IEnumerable<Secret>>> SearchSecretsAsync(string searchTerm, CancellationToken cancellationToken = default);
}
