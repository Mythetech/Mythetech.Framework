using MudBlazor;

namespace Mythetech.Framework.Components.Dialogs;

/// <summary>
/// Helper methods for creating consistent dialog configurations.
/// </summary>
public static class MythetechDialogs
{
    /// <summary>
    /// Creates default dialog options with common Mythetech styling.
    /// </summary>
    /// <param name="maxWidth">The maximum width of the dialog (default: Small)</param>
    /// <returns>Configured dialog options</returns>
    public static DialogOptions CreateDefaultOptions(MaxWidth maxWidth = MaxWidth.Small)
        => new DialogOptions
        {
            CloseOnEscapeKey = true,
            BackgroundClass = "mythetech-dialog",
            MaxWidth = maxWidth,
            FullWidth = true,
            CloseButton = true,
        };

    /// <summary>
    /// Creates dialog options for a confirmation dialog.
    /// </summary>
    /// <returns>Configured dialog options for confirmation</returns>
    public static DialogOptions CreateConfirmationOptions()
        => new DialogOptions
        {
            CloseOnEscapeKey = true,
            BackgroundClass = "mythetech-dialog",
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true,
            CloseButton = false,
        };

    /// <summary>
    /// Creates dialog options for a large dialog.
    /// </summary>
    /// <returns>Configured dialog options for large content</returns>
    public static DialogOptions CreateLargeOptions()
        => new DialogOptions
        {
            CloseOnEscapeKey = true,
            BackgroundClass = "mythetech-dialog",
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true,
        };

    /// <summary>
    /// Creates dialog options for a fullscreen dialog.
    /// </summary>
    /// <returns>Configured dialog options for fullscreen</returns>
    public static DialogOptions CreateFullscreenOptions()
        => new DialogOptions
        {
            CloseOnEscapeKey = true,
            BackgroundClass = "mythetech-dialog",
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true,
            FullScreen = true,
            CloseButton = true,
        };
}
