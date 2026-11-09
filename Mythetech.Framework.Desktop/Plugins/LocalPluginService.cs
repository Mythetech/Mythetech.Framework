using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Commands;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.Infrastructure.Plugins.Installation;
using Mythetech.Framework.Infrastructure.Plugins.Local;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Desktop.Plugins;

/// <summary>
/// Loads plugins from local build output folders and reloads them when a rebuild changes them.
/// Each load runs from a temporary copy of the build output, so the build can overwrite its files.
/// </summary>
public sealed class LocalPluginService : ILocalPluginService, IDisposable, IAsyncDisposable
{
    private const string EntriesFileName = "local-plugins.json";
    private const string CopiesFolderName = "mythetech-local-plugins";
    private const string NuGetPackagesVariable = "NUGET_PACKAGES";
    private const string UnavailableMessage = "Local plugins aren't available in this app.";
    private const string NoPluginMessage = "No plugin was found in that file.";
    private const int AppKeyLength = 12;

    // A build writes many files, so a reload waits until the watched folders have been quiet this long.
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(400);

    private readonly IMessageBus _messageBus;
    private readonly ILogger<LocalPluginService> _logger;
    private readonly string _copiesBase;
    private readonly PluginLoader? _loader;
    private readonly PluginState? _state;
    private readonly Channel<string> _changes = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
    private readonly CancellationTokenSource _disposal = new();
    private readonly CancellationToken _disposalToken;
    // Serializes every change to the tracked plugins, so reloads never overlap with each other or with adds and removes.
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, TrackedPlugin> _tracked = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<FileSystemWatcher>> _watchers = new(StringComparer.Ordinal);
    private readonly Lock _watchersLock = new();
    private volatile IReadOnlyList<LocalPluginEntry> _entries = [];
    private volatile bool _enabled;
    private List<string>? _savedPaths;
    private bool _savedLoaded;
    private Task? _readerLoop;
    private int _disposed;

    /// <summary>
    /// Constructor. Without a plugin loader and state, as in an app without the plugin framework, the service is unavailable.
    /// </summary>
    public LocalPluginService(
        IMessageBus messageBus,
        ILogger<LocalPluginService> logger,
        PluginLoader? loader = null,
        PluginState? state = null,
        ISettingsProvider? settingsProvider = null)
        : this(messageBus, logger, Path.Combine(Path.GetTempPath(), CopiesFolderName), loader, state, settingsProvider)
    {
    }

    internal LocalPluginService(
        IMessageBus messageBus,
        ILogger<LocalPluginService> logger,
        string copiesBase,
        PluginLoader? loader,
        PluginState? state,
        ISettingsProvider? settingsProvider)
    {
        _messageBus = messageBus;
        _logger = logger;
        _copiesBase = copiesBase;
        _loader = loader;
        _state = state;
        _disposalToken = _disposal.Token;
        _enabled = settingsProvider?.GetSettings<PluginSettings>()?.LocalPluginsEnabled ?? false;
    }

    /// <inheritdoc />
    public bool IsAvailable => _loader is not null && _state is not null;

    /// <inheritdoc />
    public bool IsEnabled => _enabled;

    /// <inheritdoc />
    public IReadOnlyList<LocalPluginEntry> Entries => _entries;

    /// <inheritdoc />
    public async Task LoadSavedAsync()
    {
        if (!IsAvailable || !_enabled)
            return;

        await _gate.WaitAsync(_disposalToken);
        try
        {
            await LoadSavedCoreAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetEnabledAsync(bool enabled)
    {
        await _gate.WaitAsync(_disposalToken);
        try
        {
            if (_enabled == enabled)
                return;

            _enabled = enabled;
            if (!enabled)
            {
                _savedLoaded = false;
                StopAllWatchers();
                return;
            }

            if (IsAvailable && _state!.PluginsLoaded)
                await LoadSavedCoreAsync();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<PluginInstallResult> AddAsync(string dllPath)
    {
        if (!IsAvailable)
            return Failed(UnavailableMessage);

        await _gate.WaitAsync(_disposalToken);
        try
        {
            var tracked = await LoadAndTrackAsync(Path.GetFullPath(dllPath));
            if (tracked is null)
                return Failed(NoPluginMessage);

            await SaveAsync();

            return new PluginInstallResult(true, [tracked.Plugin], $"Added local plugin {tracked.Plugin.Manifest.Name}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add the local plugin {Path}", dllPath);
            return Failed(NoPluginMessage);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(string pluginId)
    {
        if (!IsAvailable)
            return false;

        await _gate.WaitAsync(_disposalToken);
        try
        {
            if (!_tracked.TryGetValue(pluginId, out var tracked))
                return false;

            await UntrackAsync(tracked);
            await SaveAsync();
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task LoadSavedCoreAsync()
    {
        if (!_enabled || _savedLoaded)
            return;

        _savedLoaded = true;
        DeleteAllCopies();

        foreach (var path in (await GetSavedPathsAsync()).ToList())
        {
            try
            {
                if (await LoadAndTrackAsync(path) is null)
                    _logger.LogWarning("No plugin was found in the saved local plugin {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load the saved local plugin {Path}", path);
            }
        }
    }

    // Loads a copy of the build, registers it in place of any plugin with its ID, keeps its path among the saved
    // paths and watches the build folder.
    private async Task<TrackedPlugin?> LoadAndTrackAsync(string dllPath)
    {
        var samePath = _tracked.Values.FirstOrDefault(t => PathEquals(t.DllPath, dllPath));
        var loaded = await LoadCopyAsync(dllPath, samePath?.PluginId ?? Path.GetFileNameWithoutExtension(dllPath));
        if (loaded is null)
            return null;

        var (plugin, copyFolder) = loaded.Value;
        var pluginId = plugin.Manifest.Id;

        // The build at this path was renamed to another ID, so its old registration goes.
        if (samePath is not null && samePath.PluginId != pluginId)
            await UntrackAsync(samePath);

        _tracked.TryGetValue(pluginId, out var previous);
        var replaced = await _state!.ReplacePluginAsync(plugin);
        var shadowedFolder = previous is not null ? previous.ShadowedFolder : InstallFolderOf(replaced);

        var savedPaths = await GetSavedPathsAsync();
        if (previous is not null)
        {
            StopWatching(pluginId);
            TryDeleteFolder(previous.CopyFolder);
            if (!PathEquals(previous.DllPath, dllPath))
                savedPaths.RemoveAll(path => PathEquals(path, previous.DllPath));
        }

        if (!savedPaths.Any(path => PathEquals(path, dllPath)))
            savedPaths.Add(dllPath);

        var tracked = new TrackedPlugin(pluginId, dllPath, plugin, copyFolder, shadowedFolder);
        _tracked[pluginId] = tracked;
        RefreshEntries();

        if (_enabled)
            StartWatching(tracked);

        return tracked;
    }

    private async Task UntrackAsync(TrackedPlugin tracked)
    {
        StopWatching(tracked.PluginId);
        _tracked.Remove(tracked.PluginId);
        (await GetSavedPathsAsync()).RemoveAll(path => PathEquals(path, tracked.DllPath));
        RefreshEntries();

        var restored = tracked.ShadowedFolder is null ? null : await LoadInstallAsync(tracked.PluginId, tracked.ShadowedFolder);
        if (restored is not null)
            await _state!.ReplacePluginAsync(restored);
        else
            await _state!.UnregisterPluginAsync(tracked.PluginId);

        TryDeleteFolder(tracked.CopyFolder);
    }

    private async Task<PluginInfo?> LoadInstallAsync(string pluginId, string folder)
    {
        try
        {
            var plugins = await Task.Run(() => _loader!.LoadPluginsFromFolder(folder));
            return plugins.FirstOrDefault(p => p.Manifest.Id == pluginId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Couldn't load the install of plugin {PluginId} back from {Folder}", pluginId, folder);
            return null;
        }
    }

    private async Task ReloadAsync(string pluginId)
    {
        await _gate.WaitAsync(_disposalToken);
        try
        {
            if (!_enabled || !_tracked.TryGetValue(pluginId, out var tracked))
                return;

            try
            {
                var loaded = await LoadCopyAsync(tracked.DllPath, tracked.PluginId);
                if (loaded is null || loaded.Value.Plugin.Manifest.Id != tracked.PluginId)
                {
                    if (loaded is not null)
                        TryDeleteFolder(loaded.Value.CopyFolder);

                    _logger.LogWarning("No plugin with ID {PluginId} was found in the rebuilt {Path}", pluginId, tracked.DllPath);
                    await NotifyReloadFailedAsync(tracked);
                    return;
                }

                await _state!.ReplacePluginAsync(loaded.Value.Plugin);
                TryDeleteFolder(tracked.CopyFolder);
                _tracked[pluginId] = tracked with { Plugin = loaded.Value.Plugin, CopyFolder = loaded.Value.CopyFolder };
                _logger.LogInformation("Reloaded local plugin {PluginId} from {Path}", pluginId, tracked.DllPath);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !_disposalToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Failed to reload the local plugin {PluginId} from {Path}", pluginId, tracked.DllPath);
                await NotifyReloadFailedAsync(tracked);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    // Runs inside the reload's failure handling, so a failing publish must not escape the reader loop.
    private async Task NotifyReloadFailedAsync(TrackedPlugin tracked)
    {
        try
        {
            await _messageBus.PublishAsync(new AddNotification(
                $"The local plugin {tracked.Plugin.Manifest.Name} couldn't be reloaded.",
                Severity.Error));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Couldn't show that local plugin {PluginId} failed to reload", tracked.PluginId);
        }
    }

    // Copying and type-scanning the build is file and CPU work, so it stays off the caller's (UI) thread.
    private async Task<(PluginInfo Plugin, string CopyFolder)?> LoadCopyAsync(string dllPath, string copyKey)
    {
        return await Task.Run(() => LoadCopy(dllPath, copyKey), _disposalToken);
    }

    private (PluginInfo Plugin, string CopyFolder)? LoadCopy(string dllPath, string copyKey)
    {
        var copyFolder = CreateCopyFolder(copyKey);
        try
        {
            CopyDirectory(Path.GetDirectoryName(dllPath)!, copyFolder);

            var dllName = Path.GetFileName(dllPath);
            var plugin = _loader!.LoadPluginsFromFolder(copyFolder, isLocal: true)
                .FirstOrDefault(p => string.Equals(Path.GetFileName(p.SourcePath), dllName, StringComparison.OrdinalIgnoreCase));
            if (plugin is not null)
                return (plugin, copyFolder);
        }
        catch
        {
            TryDeleteFolder(copyFolder);
            throw;
        }

        TryDeleteFolder(copyFolder);
        return null;
    }

    private string CreateCopyFolder(string copyKey)
    {
        var keyFolder = Path.Combine(AppCopiesFolder(), PluginInstallLayout.SanitizeFolderName(copyKey));
        var ticks = DateTime.UtcNow.Ticks;
        string folder;
        while (Directory.Exists(folder = Path.Combine(keyFolder, ticks.ToString(CultureInfo.InvariantCulture))))
        {
            ticks++;
        }

        Directory.CreateDirectory(folder);
        return folder;
    }

    // Skips what a build deletes between the listing and the copy, such as a staging file it renames.
    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            try
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
            }
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            try
            {
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }
            catch (DirectoryNotFoundException)
            {
            }
        }
    }

    private void StartWatching(TrackedPlugin tracked)
    {
        var buildFolder = Path.GetDirectoryName(tracked.DllPath)!;
        var watchers = new List<FileSystemWatcher>();
        AddWatcher(watchers, buildFolder, tracked.PluginId, includeSubdirectories: false);

        var manifestPath = Path.Combine(buildFolder, $"{Path.GetFileNameWithoutExtension(tracked.DllPath)}.staticwebassets.runtime.json");
        var contentRoots = StaticWebAssetsManifest.TryLoad(manifestPath)?.ContentRoots ?? [];
        var nuGetPackages = NuGetPackagesFolder();
        foreach (var root in contentRoots.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Directory.Exists(root) && !PluginInstallLayout.IsInside(root, nuGetPackages))
                AddWatcher(watchers, root, tracked.PluginId, includeSubdirectories: true);
        }

        lock (_watchersLock)
        {
            if (_disposed != 0)
            {
                DisposeWatchers(watchers);
                return;
            }

            _watchers[tracked.PluginId] = watchers;
            // The one loop this service owns, cancelled on dispose; Task.Run keeps it off the caller's (UI) thread.
            _readerLoop ??= Task.Run(() => ReadChangesAsync(_disposalToken));
        }
    }

    private void AddWatcher(List<FileSystemWatcher> watchers, string folder, string pluginId, bool includeSubdirectories)
    {
        FileSystemWatcher? watcher = null;
        try
        {
            watcher = new FileSystemWatcher(folder) { IncludeSubdirectories = includeSubdirectories };
            watcher.Changed += (_, _) => _changes.Writer.TryWrite(pluginId);
            watcher.Created += (_, _) => _changes.Writer.TryWrite(pluginId);
            watcher.Deleted += (_, _) => _changes.Writer.TryWrite(pluginId);
            watcher.Renamed += (_, _) => _changes.Writer.TryWrite(pluginId);
            // An error can mean dropped events, so reload rather than miss a change.
            watcher.Error += (_, _) => _changes.Writer.TryWrite(pluginId);
            watcher.EnableRaisingEvents = true;
            watchers.Add(watcher);
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            watcher?.Dispose();
            _logger.LogWarning(ex, "Can't watch {Folder} for changes to local plugin {PluginId}", folder, pluginId);
        }
    }

    private void StopWatching(string pluginId)
    {
        lock (_watchersLock)
        {
            if (_watchers.Remove(pluginId, out var watchers))
                DisposeWatchers(watchers);
        }
    }

    private void StopAllWatchers()
    {
        lock (_watchersLock)
        {
            foreach (var watchers in _watchers.Values)
            {
                DisposeWatchers(watchers);
            }

            _watchers.Clear();
        }
    }

    private static void DisposeWatchers(List<FileSystemWatcher> watchers)
    {
        foreach (var watcher in watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
    }

    // Collects plugin IDs until the folders have been quiet for QuietPeriod, then reloads each in turn.
    private async Task ReadChangesAsync(CancellationToken cancellationToken)
    {
        var pending = new List<string>();
        try
        {
            while (await _changes.Reader.WaitToReadAsync(cancellationToken))
            {
                DrainChanges(pending);
                do
                {
                    await Task.Delay(QuietPeriod, cancellationToken);
                }
                while (DrainChanges(pending));

                foreach (var pluginId in pending)
                {
                    await ReloadAsync(pluginId);
                }

                pending.Clear();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local plugins stopped reloading");
        }
    }

    private bool DrainChanges(List<string> pending)
    {
        var received = false;
        while (_changes.Reader.TryRead(out var pluginId))
        {
            received = true;
            if (!pending.Contains(pluginId))
                pending.Add(pluginId);
        }

        return received;
    }

    private string EntriesFilePath => Path.Combine(_state!.ResolvePluginDirectory(), EntriesFileName);

    // Read once and kept, so a saved path that doesn't load at startup stays saved for the next one.
    private async Task<List<string>> GetSavedPathsAsync()
    {
        if (_savedPaths is not null)
            return _savedPaths;

        _savedPaths = [];
        var path = EntriesFilePath;
        try
        {
            if (File.Exists(path))
                _savedPaths = (JsonSerializer.Deserialize<List<string?>>(await File.ReadAllTextAsync(path)) ?? []).OfType<string>().ToList();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Couldn't read the saved local plugins from {Path}", path);
        }

        return _savedPaths;
    }

    private async Task SaveAsync()
    {
        var path = EntriesFilePath;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(await GetSavedPathsAsync()));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(ex, "Couldn't save the local plugins to {Path}", path);
        }
    }

    private void RefreshEntries() =>
        _entries = _tracked.Values.Select(t => new LocalPluginEntry(t.PluginId, t.DllPath)).ToArray();

    private static string? InstallFolderOf(PluginInfo? plugin) =>
        plugin is { IsLocal: false, SourcePath: { } sourcePath } ? Path.GetDirectoryName(Path.GetFullPath(sourcePath)) : null;

    private static string NuGetPackagesFolder() =>
        System.Environment.GetEnvironmentVariable(NuGetPackagesVariable) is { Length: > 0 } configured
            ? configured
            : Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".nuget", "packages");

    // Paths are full paths, saved as AddAsync resolved them.
    private static bool PathEquals(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private string AppCopiesFolder() => AppCopiesFolder(_copiesBase, _state!.ResolvePluginDirectory());

    // Keyed by the app's plugin directory, so one app's startup never deletes copies another running app loaded.
    internal static string AppCopiesFolder(string copiesBase, string pluginDirectory)
    {
        var normalized = Path.TrimEndingDirectorySeparator(Path.GetFullPath(pluginDirectory));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Path.Combine(copiesBase, Convert.ToHexStringLower(hash)[..AppKeyLength]);
    }

    private void DeleteAllCopies()
    {
        var appFolder = AppCopiesFolder();
        try
        {
            if (!Directory.Exists(appFolder))
                return;

            foreach (var keyFolder in Directory.GetDirectories(appFolder))
            {
                TryDeleteFolder(keyFolder);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Couldn't delete the local plugin copies in {Folder}", appFolder);
        }
    }

    // A loaded copy can't be deleted on Windows; the next startup deletes it.
    private void TryDeleteFolder(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogDebug(ex, "Couldn't delete the local plugin copy {Folder}", folder);
        }
    }

    private static PluginInstallResult Failed(string message) => new(false, [], message);

    private bool BeginDispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return false;

        _disposal.Cancel();
        _changes.Writer.TryComplete();
        StopAllWatchers();
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (BeginDispose())
            _disposal.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!BeginDispose())
            return;

        Task? readerLoop;
        lock (_watchersLock)
        {
            readerLoop = _readerLoop;
        }

        if (readerLoop is not null)
            await readerLoop;

        _disposal.Dispose();
    }

    /// <summary>A local plugin: its build DLL, its registered plugin, the copy it loaded from and the install it replaced.</summary>
    private sealed record TrackedPlugin(string PluginId, string DllPath, PluginInfo Plugin, string CopyFolder, string? ShadowedFolder);
}
