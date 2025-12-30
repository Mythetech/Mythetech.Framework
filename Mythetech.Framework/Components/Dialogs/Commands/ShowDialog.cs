using MudBlazor;

namespace Mythetech.Framework.Components.Dialogs.Commands;

/// <summary>
/// Command to show a dialog via the MessageBus
/// </summary>
public record ShowDialog(Type Dialog, string Title, DialogOptions? Options = default, DialogParameters? Parameters = default);

