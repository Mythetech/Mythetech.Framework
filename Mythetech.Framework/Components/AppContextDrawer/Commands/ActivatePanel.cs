namespace Mythetech.Framework.Components.AppContextDrawer.Commands;

/// <summary>
/// Message bus command to show a specific panel in the AppContextDrawer. The drawer opens if it is collapsed
/// and switches from any other panel. A panel that is already on screen stays as it is, so the command never
/// closes the drawer.
/// </summary>
/// <param name="PanelId">The ID of the panel to activate.</param>
public record ActivatePanel(string PanelId);
