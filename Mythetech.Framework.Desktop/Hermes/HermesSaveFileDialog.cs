using Hermes.Abstractions;

namespace Mythetech.Framework.Desktop.Hermes;

/// <summary>
/// Shows the native save file dialog
/// </summary>
internal interface ISaveFileDialog
{
    /// <summary>
    /// Shows the dialog and returns the chosen path, or null when the user cancels
    /// </summary>
    /// <param name="fileName">File name suggested in the dialog</param>
    /// <param name="extension">Extension to filter by without the leading dot, or null for no filter</param>
    string? ShowSaveFile(string fileName, string? extension);
}

/// <summary>
/// Hermes implementation of <see cref="ISaveFileDialog"/> using the main window's dialogs
/// </summary>
internal sealed class HermesSaveFileDialog : ISaveFileDialog
{
    private readonly IHermesAppProvider _provider;

    public HermesSaveFileDialog(IHermesAppProvider provider)
    {
        _provider = provider;
    }

    public string? ShowSaveFile(string fileName, string? extension)
    {
        var dialogs = _provider.Instance.MainWindow.Dialogs;

        DialogFilter[]? filters = extension is null
            ? null
            : [new DialogFilter($"{extension.ToUpperInvariant()} files", [extension])];

        return dialogs.ShowSaveFile("Save File", null, filters, fileName);
    }
}
