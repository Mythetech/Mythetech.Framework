namespace Mythetech.Framework.Components.AppContextDrawer.Commands;

/// <summary>
/// Message bus command to activate a specific panel in the AppContextDrawer.
/// </summary>
/// <param name="PanelId">The ID of the panel to activate.</param>
public record ActivatePanel(string PanelId);
