namespace Mythetech.Framework.Infrastructure.Settings.Events;

/// <summary>
/// Event published when any settings model changes.
/// Consumers can subscribe to this for generic settings handling (e.g., persistence).
/// </summary>
/// <param name="Settings">The settings instance that changed.</param>
public record SettingsModelChanged(SettingsBase Settings);

/// <summary>
/// Typed event for domain-specific settings changes.
/// Allows consumers to subscribe only to their specific settings type.
/// </summary>
/// <typeparam name="T">The specific settings type.</typeparam>
/// <param name="Settings">The settings instance that changed.</param>
public record SettingsModelChanged<T>(T Settings) where T : SettingsBase;
