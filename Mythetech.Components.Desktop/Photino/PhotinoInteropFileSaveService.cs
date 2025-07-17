using Mythetech.Components.Infrastructure;

namespace Mythetech.Components.Desktop.Photino;

public class PhotinoInteropFileSaveService : IFileSaveService
{
    private readonly IPhotinoAppProvider _provider;

    public PhotinoInteropFileSaveService(IPhotinoAppProvider provider)
    {
        _provider = provider;
    }
    
    public async Task<bool> SaveFileAsync(string fileName, string data)
    {
        string? location = await PromptFileSaveAsync(fileName);

        if (string.IsNullOrWhiteSpace(location))
            return false;
        
        await File.WriteAllTextAsync(location, data);
        return true;
    }

    public async Task<string?> PromptFileSaveAsync(string fileName, string extension = "txt")
    {
        var app = _provider.Instance;

        string? location = await app.MainWindow.ShowSaveFileAsync("Save File", null, [(fileName, [extension])]);
        
        return string.IsNullOrEmpty(location) ? null : location;
    }
}