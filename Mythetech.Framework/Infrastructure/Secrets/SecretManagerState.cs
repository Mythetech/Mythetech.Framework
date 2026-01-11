namespace Mythetech.Framework.Infrastructure.Secrets;

/// <summary>
/// Central state for managing secrets. UI components should depend on this.
/// Registered as a Singleton in DI.
/// </summary>
public class SecretManagerState : IDisposable
{
    private readonly List<Secret> _secrets = [];
    private readonly List<ISecretManager> _availableManagers = [];
    private ISecretManager? _currentManager;
    private bool _disposed;

    /// <summary>
    /// Raised when any secret state changes (secrets refreshed, manager registered, etc.)
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// All cached secrets
    /// </summary>
    public IReadOnlyList<Secret> Secrets => _secrets.AsReadOnly();

    /// <summary>
    /// All registered secret managers
    /// </summary>
    public IReadOnlyList<ISecretManager> AvailableManagers => _availableManagers.AsReadOnly();

    /// <summary>
    /// Currently active secret manager
    /// </summary>
    public ISecretManager? CurrentManager
    {
        get => _currentManager;
        private set
        {
            if (_currentManager != value)
            {
                _currentManager = value;
                _secrets.Clear();
                NotifyStateChanged();
            }
        }
    }

    /// <summary>
    /// Whether any secret manager is available
    /// </summary>
    public bool IsAvailable => _availableManagers.Count > 0;

    /// <summary>
    /// Whether the current manager is active/set
    /// </summary>
    public bool HasActiveManager => _currentManager != null;

    /// <summary>
    /// Register a secret manager to the available list
    /// </summary>
    public void RegisterManager(ISecretManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        if (!_availableManagers.Contains(manager))
        {
            _availableManagers.Add(manager);
            NotifyStateChanged();
        }

        // If no active manager, set this as the active one
        if (_currentManager == null)
        {
            CurrentManager = manager;
        }
    }

    /// <summary>
    /// Set the active secret manager
    /// </summary>
    public void SetActiveManager(ISecretManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        if (!_availableManagers.Contains(manager))
        {
            throw new InvalidOperationException($"Manager '{manager.Name}' is not registered. Register it first using RegisterManager.");
        }

        CurrentManager = manager;
    }

    /// <summary>
    /// Set the active secret manager by name
    /// </summary>
    public void SetActiveManager(string managerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(managerName);

        var manager = _availableManagers.FirstOrDefault(m =>
            m.Name.Equals(managerName, StringComparison.OrdinalIgnoreCase));

        if (manager == null)
        {
            throw new InvalidOperationException($"Manager '{managerName}' not found. Available managers: {string.Join(", ", _availableManagers.Select(m => m.Name))}");
        }

        CurrentManager = manager;
    }

    /// <summary>
    /// Clear the cached secrets
    /// </summary>
    public void ClearSecrets()
    {
        _secrets.Clear();
        NotifyStateChanged();
    }

    /// <summary>
    /// Check if the current manager supports searching/listing secrets
    /// </summary>
    public bool CanSearch() => _currentManager is ISecretSearcher;

    /// <summary>
    /// Check if a specific manager supports searching/listing secrets
    /// </summary>
    public bool CanSearch(ISecretManager manager) => manager is ISecretSearcher;

    /// <summary>
    /// Check if the current manager supports writing secrets
    /// </summary>
    public bool CanWrite() => _currentManager is ISecretWriter;

    /// <summary>
    /// Check if a specific manager supports writing secrets
    /// </summary>
    public bool CanWrite(ISecretManager manager) => manager is ISecretWriter;

    /// <summary>
    /// Refresh secrets from the manager (only if manager supports ISecretSearcher)
    /// </summary>
    public async Task<SecretOperationResult> RefreshSecretsAsync()
    {
        if (_currentManager == null)
        {
            _secrets.Clear();
            NotifyStateChanged();
            return SecretOperationResult.Fail(
                "No secret manager is active.",
                SecretOperationErrorKind.ConnectionFailed);
        }

        if (_currentManager is not ISecretSearcher searcher)
        {
            _secrets.Clear();
            NotifyStateChanged();
            return SecretOperationResult.Fail(
                $"'{_currentManager.Name}' does not support listing secrets.",
                SecretOperationErrorKind.NotSupported);
        }

        var result = await searcher.ListSecretsAsync();
        _secrets.Clear();

        if (result.Success && result.Value != null)
        {
            _secrets.AddRange(result.Value);
        }

        NotifyStateChanged();
        return result.Success
            ? SecretOperationResult.Ok()
            : SecretOperationResult.Fail(result.ErrorMessage ?? "Unknown error", result.ErrorKind ?? SecretOperationErrorKind.Unknown);
    }

    /// <summary>
    /// Get secret (from cache or manager)
    /// </summary>
    public async Task<SecretOperationResult<Secret>> GetSecretAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return SecretOperationResult<Secret>.Fail(
                "Key cannot be null or empty.",
                SecretOperationErrorKind.InvalidKey);
        }

        var cached = _secrets.FirstOrDefault(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (cached != null && !string.IsNullOrEmpty(cached.Value))
        {
            return SecretOperationResult<Secret>.Ok(cached);
        }

        if (_currentManager == null)
        {
            return SecretOperationResult<Secret>.Fail(
                "No secret manager is active.",
                SecretOperationErrorKind.ConnectionFailed);
        }

        var result = await _currentManager.GetSecretAsync(key);

        if (result.Success && result.Value != null)
        {
            var existingIndex = _secrets.FindIndex(s => s.Key.Equals(result.Value.Key, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                _secrets[existingIndex] = result.Value;
            }
            else
            {
                _secrets.Add(result.Value);
            }
            NotifyStateChanged();
        }

        return result;
    }

    /// <summary>
    /// Search cached secrets
    /// </summary>
    public Task<IEnumerable<Secret>> SearchSecretsAsync(string searchTerm)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchTerm);

        var term = searchTerm.ToLowerInvariant();
        var results = _secrets.Where(s =>
            s.Key.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            (s.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (s.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (s.Tags?.Any(t => t.Contains(term, StringComparison.OrdinalIgnoreCase)) ?? false) ||
            (s.Category?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
        );

        return Task.FromResult(results);
    }

    /// <summary>
    /// Notify listeners that state has changed
    /// </summary>
    public void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _secrets.Clear();
        _availableManagers.Clear();
        _currentManager = null;
        GC.SuppressFinalize(this);
    }
}

