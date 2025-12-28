using Mythetech.Framework.Infrastructure;

namespace Mythetech.Framework.Desktop.Photino;

/// <summary>
/// Photino desktop implementation of file and folder open dialogs
/// </summary>
public class PhotinoInteropFileOpenService : IFileOpenService
{
    private readonly IPhotinoAppProvider _provider;

    /// <summary>
    /// Creates a new instance of the Photino file open service
    /// </summary>
    /// <param name="provider">The Photino app provider for accessing the main window</param>
    public PhotinoInteropFileOpenService(IPhotinoAppProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    public async Task<string[]> OpenFileAsync(
        string title = "Choose file",
        string? defaultPath = null,
        bool multiSelect = false,
        FileFilter[]? filters = null)
    {
        var app = _provider.Instance;

        var photinoFilters = filters?
            .Select(f => (f.Name, f.Extensions))
            .ToArray();

        var result = await app.MainWindow.ShowOpenFileAsync(title, defaultPath, multiSelect, photinoFilters);

        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<string[]> OpenFolderAsync(
        string title = "Choose folder",
        string? defaultPath = null,
        bool multiSelect = false)
    {
        var app = _provider.Instance;

        var result = await app.MainWindow.ShowOpenFolderAsync(title, defaultPath, multiSelect);

        return result ?? [];
    }
}

