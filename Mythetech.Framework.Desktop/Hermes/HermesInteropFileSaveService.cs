using Hermes.Abstractions;
using Mythetech.Framework.Infrastructure.Files;

namespace Mythetech.Framework.Desktop.Hermes;

/// <summary>
/// Hermes desktop implementation of file save dialogs
/// </summary>
public class HermesInteropFileSaveService : IFileSaveService
{
    private readonly IHermesAppProvider _provider;

    /// <summary>
    /// Creates a new instance of the Hermes file save service
    /// </summary>
    /// <param name="provider">The Hermes app provider for accessing the main window</param>
    public HermesInteropFileSaveService(IHermesAppProvider provider)
    {
        _provider = provider;
    }

    /// <inheritdoc />
    public async Task<bool> SaveFileAsync(string fileName, string data)
    {
        string? location = await PromptFileSaveAsync(fileName);

        if (string.IsNullOrWhiteSpace(location))
            return false;

        await File.WriteAllTextAsync(location, data);
        return true;
    }

    /// <inheritdoc />
    public Task<string?> PromptFileSaveAsync(string fileName, string extension = "txt")
    {
        var dialogs = _provider.Instance.MainWindow.Dialogs;

        var filters = new[] { new DialogFilter(fileName, [extension]) };
        var location = dialogs.ShowSaveFile("Save File", null, filters, fileName);

        return Task.FromResult(string.IsNullOrEmpty(location) ? null : location);
    }
}
