using Microsoft.Extensions.Logging;

namespace Mythetech.Framework.Infrastructure.Plugins.Installation;

/// <summary>
/// Installs plugins into new folders under the plugins directory, loads each into a fresh context and swaps
/// the registration, then marks the replaced plugin's folder for deletion at the next startup.
/// </summary>
public sealed class PluginInstaller : IPluginInstaller
{
    private const string ZipExtension = ".zip";
    private const string DllExtension = ".dll";

    private const string NoPluginInPackageMessage = "That package doesn't contain a plugin.";
    private const string DownloadFailedMessage = "Couldn't download the plugin. Check your connection and try again.";
    private const string InvalidPackageMessage = "That download isn't a plugin package.";
    private const string DownloadsUnavailableMessage = "Plugin downloads aren't available in this app.";
    private const string InstallFailedMessage = "The plugin couldn't be installed.";
    private const string NoPluginInFileMessage = "No plugin was found in that file.";

    private readonly PluginState _state;
    private readonly PluginLoader _loader;
    private readonly ILogger<PluginInstaller> _logger;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly HttpClient? _httpClient;

    /// <summary>
    /// Constructor. Downloads use <paramref name="httpClientFactory"/> when available, otherwise <paramref name="httpClient"/>.
    /// </summary>
    public PluginInstaller(
        PluginState state,
        PluginLoader loader,
        ILogger<PluginInstaller> logger,
        IHttpClientFactory? httpClientFactory = null,
        HttpClient? httpClient = null)
    {
        _state = state;
        _loader = loader;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<PluginInstallResult> InstallFromUrlAsync(string url, string pluginKey, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory?.CreateClient() ?? _httpClient;
        if (client is null)
            return Failed(DownloadsUnavailableMessage);

        await using var package = new MemoryStream();
        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            await response.Content.CopyToAsync(package, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to download plugin {PluginKey} from {Url}", pluginKey, url);
            return Failed(DownloadFailedMessage);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Failed to download plugin {PluginKey} from {Url}", pluginKey, url);
            return Failed(InstallFailedMessage);
        }

        package.Position = 0;
        return await InstallFromStreamAsync(package, GetFileNameFromUrl(url), pluginKey);
    }

    /// <inheritdoc />
    public async Task<PluginInstallResult> InstallFromStreamAsync(Stream package, string fileName, string pluginKey)
    {
        ArgumentNullException.ThrowIfNull(package);

        try
        {
            var pluginsDirectory = _state.ResolvePluginDirectory();
            var installFolder = PluginInstallLayout.CreateInstallFolder(pluginsDirectory, pluginKey);
            await WritePackageAsync(package, fileName, pluginKey, installFolder);

            var plugins = _loader.LoadPluginsFromFolder(installFolder);
            if (plugins.Count == 0)
                return Failed(NoPluginInPackageMessage);

            // The folder is created marked, so every failure before this point leaves it for the next startup to delete.
            File.Delete(Path.Combine(installFolder, PluginInstallLayout.RemovedMarkerFileName));

            var replacedAny = false;
            foreach (var plugin in plugins)
            {
                var previous = await _state.ReplacePluginAsync(plugin);
                if (previous is null)
                    continue;

                replacedAny = true;
                RetirePreviousFolder(previous, pluginsDirectory, installFolder);
            }

            return new PluginInstallResult(true, plugins, SuccessMessage(plugins, replacedAny));
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "The package for plugin {PluginKey} isn't a valid plugin package", pluginKey);
            return Failed(InvalidPackageMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install plugin {PluginKey}", pluginKey);
            return Failed(InstallFailedMessage);
        }
    }

    /// <inheritdoc />
    public async Task<PluginInstallResult> ImportFromFileAsync(string dllPath)
    {
        try
        {
            var plugin = _loader.LoadPlugin(dllPath);
            if (plugin is null)
                return Failed(NoPluginInFileMessage);

            var previous = await _state.ReplacePluginAsync(plugin);
            return new PluginInstallResult(true, [plugin], SuccessMessage([plugin], previous is not null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import plugin from {Path}", dllPath);
            return Failed(InstallFailedMessage);
        }
    }

    private static async Task WritePackageAsync(Stream package, string fileName, string pluginKey, string installFolder)
    {
        // Detecting a zip reads ahead, which needs a stream that can seek back.
        if (!package.CanSeek)
        {
            await using var buffer = new MemoryStream();
            await package.CopyToAsync(buffer);
            buffer.Position = 0;
            await WritePackageAsync(buffer, fileName, pluginKey, installFolder);
            return;
        }

        if (fileName.EndsWith(ZipExtension, StringComparison.OrdinalIgnoreCase) || await PluginPackageExtractor.IsZipAsync(package))
        {
            PluginPackageExtractor.Extract(package, installFolder);
            return;
        }

        await using var file = File.Create(Path.Combine(installFolder, GetDllFileName(fileName, pluginKey)));
        await package.CopyToAsync(file);
    }

    private static string GetDllFileName(string fileName, string pluginKey)
    {
        var name = Path.GetFileName(fileName);
        return name.EndsWith(DllExtension, StringComparison.OrdinalIgnoreCase)
            ? name
            : $"{PluginInstallLayout.SanitizeFolderName(pluginKey)}{DllExtension}";
    }

    private void RetirePreviousFolder(PluginInfo previous, string pluginsDirectory, string installFolder)
    {
        if (string.IsNullOrEmpty(previous.SourcePath))
            return;

        var previousFolder = Path.GetDirectoryName(Path.GetFullPath(previous.SourcePath));
        if (previousFolder is null
            || !PluginInstallLayout.IsInside(previousFolder, pluginsDirectory)
            || string.Equals(previousFolder, installFolder, StringComparison.OrdinalIgnoreCase))
            return;

        TryMarkForRemoval(previousFolder);
    }

    private void TryMarkForRemoval(string folder)
    {
        try
        {
            PluginInstallLayout.MarkForRemoval(folder);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not mark plugin folder {Folder} for removal", folder);
        }
    }

    private static string SuccessMessage(IReadOnlyList<PluginInfo> plugins, bool replacedAny)
    {
        if (plugins.Count > 1)
            return $"Installed {plugins.Count} plugins.";

        var manifest = plugins[0].Manifest;
        return replacedAny
            ? $"Updated {manifest.Name} to {manifest.Version}."
            : $"Installed {manifest.Name} {manifest.Version}.";
    }

    private static string GetFileNameFromUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? Path.GetFileName(uri.LocalPath) : string.Empty;

    private static PluginInstallResult Failed(string message) => new(false, [], message);
}
