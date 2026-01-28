using Hermes.Abstractions;
using Mythetech.Framework.Infrastructure.Files;

namespace Mythetech.Framework.Desktop.Hermes;

/// <summary>
/// Hermes desktop implementation of file and folder open dialogs
/// </summary>
public class HermesInteropFileOpenService : IFileOpenService
{
    private readonly IHermesAppProvider _provider;

    /// <summary>
    /// Creates a new instance of the Hermes file open service
    /// </summary>
    /// <param name="provider">The Hermes app provider for accessing the main window</param>
    public HermesInteropFileOpenService(IHermesAppProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    public Task<string[]> OpenFileAsync(
        string title = "Choose file",
        string? defaultPath = null,
        bool multiSelect = false,
        FileFilter[]? filters = null)
    {
        var dialogs = _provider.Instance.MainWindow.Dialogs;

        var hermesFilters = filters?
            .Select(f => new DialogFilter(f.Name, f.Extensions))
            .ToArray();

        var result = dialogs.ShowOpenFile(title, defaultPath, multiSelect, hermesFilters);

        return Task.FromResult(result ?? []);
    }

    /// <inheritdoc />
    public Task<string[]> OpenFolderAsync(
        string title = "Choose folder",
        string? defaultPath = null,
        bool multiSelect = false)
    {
        var dialogs = _provider.Instance.MainWindow.Dialogs;
        var result = dialogs.ShowOpenFolder(title, defaultPath, multiSelect);

        return Task.FromResult(result ?? []);
    }
}
