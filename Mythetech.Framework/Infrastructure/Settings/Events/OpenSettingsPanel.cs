namespace Mythetech.Framework.Infrastructure.Settings.Events;

/// <summary>
/// Command event to request opening the settings panel.
/// Consuming applications handle this to show their settings dialog wrapper.
/// </summary>
/// <param name="InitialSection">Optional section ID to scroll to when opening.</param>
public record OpenSettingsPanel(string? InitialSection = null);
