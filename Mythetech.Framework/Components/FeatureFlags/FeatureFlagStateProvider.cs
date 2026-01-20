namespace Mythetech.Framework.Components.FeatureFlags;

/// <summary>
/// Provides information about the current feature flag state.
/// </summary>
public abstract class FeatureFlagStateProvider
{
    /// <summary>
    /// Asynchronously gets the current <see cref="FeatureFlagState"/>.
    /// </summary>
    public abstract Task<FeatureFlagState> GetFeatureFlagState();

    /// <summary>
    /// An event that provides notification when the <see cref="FeatureFlagState"/>
    /// has changed. For example, this event may be raised when flags are toggled.
    /// </summary>
    public event FeatureFlagStateHandler? FeatureFlagStateChanged;

    /// <summary>
    /// Raises the <see cref="FeatureFlagStateChanged"/> event.
    /// </summary>
    /// <param name="task">A <see cref="Task"/> that supplies the updated <see cref="FeatureFlagState"/>.</param>
    protected void NotifyFeatureFlagStateChanged(Task<FeatureFlagState> task)
    {
        ArgumentNullException.ThrowIfNull(task);
        FeatureFlagStateChanged?.Invoke(task);
    }
}

/// <summary>
/// A handler for the <see cref="FeatureFlagStateProvider.FeatureFlagStateChanged"/> event.
/// </summary>
/// <param name="task">A <see cref="Task"/> that supplies the updated <see cref="FeatureFlagState"/>.</param>
public delegate void FeatureFlagStateHandler(Task<FeatureFlagState> task);
