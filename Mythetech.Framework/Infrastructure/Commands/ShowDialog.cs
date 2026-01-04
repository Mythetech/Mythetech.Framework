using MudBlazor;

namespace Mythetech.Framework.Infrastructure.Commands;

/// <summary>
/// Command message to display a dialog.
/// </summary>
/// <param name="Dialog">The type of dialog component to display</param>
/// <param name="Title">The title of the dialog</param>
/// <param name="Options">Optional dialog options</param>
/// <param name="Parameters">Optional dialog parameters</param>
public record ShowDialog(
    Type Dialog,
    string Title,
    DialogOptions? Options = default,
    DialogParameters? Parameters = default);
