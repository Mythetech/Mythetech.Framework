namespace Mythetech.Framework.Infrastructure.Secrets;

/// <summary>
/// Central state for managing secrets. UI components should depend on this.
/// Registered as a Singleton in DI.
/// </summary>
public class SecretManagerState : IDisposable
{
    private readonly List<Secret> _secrets = [];
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
    /// Currently registered secret manager
    /// </summary>
    public ISecretManager? CurrentManager
    {
        get => _currentManager;
        private set
        {
            if (_currentManager != value)
            {
                _currentManager = value;
                NotifyStateChanged();
            }
        }
    }
    
    /// <summary>
    /// Whether a secret manager is configured (does not test connection, just checks if registered)
    /// </summary>
    public bool IsAvailable => _currentManager != null;
    
    /// <summary>
    /// Register a secret manager
    /// </summary>
    public void RegisterManager(ISecretManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        CurrentManager = manager;
    }
    
    /// <summary>
    /// Refresh secrets from the manager
    /// </summary>
    public async Task RefreshSecretsAsync()
    {
        if (_currentManager == null)
        {
            _secrets.Clear();
            NotifyStateChanged();
            return;
        }
        
        try
        {
            var secrets = await _currentManager.ListSecretsAsync();
            _secrets.Clear();
            _secrets.AddRange(secrets);
            NotifyStateChanged();
        }
        catch
        {
            _secrets.Clear();
            NotifyStateChanged();
        }
    }
    
    /// <summary>
    /// Get secret (from cache or manager)
    /// </summary>
    public async Task<Secret?> GetSecretAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        var cached = _secrets.FirstOrDefault(s => s.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        if (cached != null)
        {
            return cached;
        }
        
        if (_currentManager == null)
        {
            return null;
        }
        
        try
        {
            var secret = await _currentManager.GetSecretAsync(key);
            if (secret != null && !_secrets.Any(s => s.Key.Equals(secret.Key, StringComparison.OrdinalIgnoreCase)))
            {
                _secrets.Add(secret);
                NotifyStateChanged();
            }
            return secret;
        }
        catch
        {
            return null;
        }
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
        _currentManager = null;
        GC.SuppressFinalize(this);
    }
}

