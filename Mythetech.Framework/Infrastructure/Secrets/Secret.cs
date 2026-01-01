namespace Mythetech.Framework.Infrastructure.Secrets;

/// <summary>
/// Represents a secret with optional metadata
/// </summary>
public class Secret
{
    /// <summary>
    /// Unique identifier/key for the secret
    /// </summary>
    public required string Key { get; init; }
    
    /// <summary>
    /// The secret value
    /// </summary>
    public required string Value { get; init; }
    
    /// <summary>
    /// Display name for the secret
    /// </summary>
    public string? Name { get; init; }
    
    /// <summary>
    /// Description of the secret
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// Tags for categorization
    /// </summary>
    public string[]? Tags { get; init; }
    
    /// <summary>
    /// Category grouping
    /// </summary>
    public string? Category { get; init; }
}

