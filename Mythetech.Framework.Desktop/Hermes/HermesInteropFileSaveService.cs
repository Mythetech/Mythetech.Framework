using Mythetech.Framework.Infrastructure.Files;

namespace Mythetech.Framework.Desktop.Hermes;

/// <summary>
/// Hermes desktop implementation of file save dialogs
/// </summary>
public class HermesInteropFileSaveService : IFileSaveService
{
    private readonly ISaveFileDialog _dialog;

    /// <summary>
    /// Creates a new instance of the Hermes file save service
    /// </summary>
    /// <param name="provider">The Hermes app provider for accessing the main window</param>
    public HermesInteropFileSaveService(IHermesAppProvider provider)
        : this(new HermesSaveFileDialog(provider))
    {
    }

    internal HermesInteropFileSaveService(ISaveFileDialog dialog)
    {
        _dialog = dialog;
    }

    /// <inheritdoc />
    public async Task<bool> SaveFileAsync(string fileName, string data)
    {
        var location = PromptForLocation(fileName);

        if (location is null)
            return false;

        await File.WriteAllTextAsync(location, data);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SaveFileAsync(string fileName, byte[] data)
    {
        var location = PromptForLocation(fileName);

        if (location is null)
            return false;

        await File.WriteAllBytesAsync(location, data);
        return true;
    }

    /// <inheritdoc />
    public Task<string?> PromptFileSaveAsync(string fileName, string extension = "txt")
    {
        var location = _dialog.ShowSaveFile(fileName, extension);

        return Task.FromResult(string.IsNullOrEmpty(location) ? null : location);
    }

    private string? PromptForLocation(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.');
        var location = _dialog.ShowSaveFile(fileName, extension.Length > 0 ? extension : null);

        return string.IsNullOrWhiteSpace(location) ? null : location;
    }
}
