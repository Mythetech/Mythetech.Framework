using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mythetech.Framework.Desktop.Updates.Events;
using Mythetech.Framework.Infrastructure.MessageBus;
using Velopack;

namespace Mythetech.Framework.Desktop.Updates;

/// <summary>
/// Velopack-based implementation of the update service.
/// Publishes events via message bus for UI components to consume.
/// </summary>
public class VelopackUpdateService : IUpdateService
{
    private readonly UpdateServiceOptions _options;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<VelopackUpdateService> _logger;
    private UpdateManager? _updateManager;

    public VelopackUpdateService(
        IOptions<UpdateServiceOptions> options,
        IMessageBus messageBus,
        ILogger<VelopackUpdateService> logger)
    {
        _options = options.Value;
        _messageBus = messageBus;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsInstalled => GetUpdateManagerSafe()?.IsInstalled ?? false;

    /// <inheritdoc />
    public Version? CurrentVersion
    {
        get
        {
            var semVer = GetUpdateManagerSafe()?.CurrentVersion;
            if (semVer is null) return null;
            return new Version(semVer.Major, semVer.Minor, semVer.Patch);
        }
    }

    /// <inheritdoc />
    public UpdateInfo? AvailableUpdate { get; private set; }

    private UpdateManager? GetUpdateManagerSafe()
    {
        try
        {
            return GetUpdateManager();
        }
        catch
        {
            return null;
        }
    }

    private UpdateManager GetUpdateManager()
    {
        if (_updateManager is not null)
            return _updateManager;

        if (string.IsNullOrEmpty(_options.UpdateUrl))
            throw new InvalidOperationException("Update URL not configured. Call AddUpdateService with a valid URL.");

        var options = new UpdateOptions
        {
            AllowVersionDowngrade = _options.AllowDowngrade,
            ExplicitChannel = _options.Channel
        };

        _updateManager = new UpdateManager(_options.UpdateUrl, options);

        return _updateManager;
    }

    /// <inheritdoc />
    public async Task CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var manager = GetUpdateManagerSafe();
        if (manager is null || !manager.IsInstalled)
        {
            _logger.LogDebug("Update check skipped - app is not installed via Velopack");
            await _messageBus.PublishAsync(new UpdateCheckCompleted(null));
            return;
        }

        await _messageBus.PublishAsync(new UpdateCheckStarted());

        try
        {
            var update = await manager.CheckForUpdatesAsync();

            if (update is null)
            {
                _logger.LogDebug("No updates available");
                AvailableUpdate = null;
                await _messageBus.PublishAsync(new UpdateCheckCompleted(null));
                return;
            }

            var semVer = update.TargetFullRelease.Version;
            var updateInfo = new UpdateInfo
            {
                TargetVersion = new Version(semVer.Major, semVer.Minor, semVer.Patch),
                Size = update.TargetFullRelease.Size,
                ReleaseNotes = update.TargetFullRelease.NotesMarkdown,
                VelopackUpdate = update
            };

            AvailableUpdate = updateInfo;
            _logger.LogInformation("Update available: {Version}", updateInfo.TargetVersion);

            await _messageBus.PublishAsync(new UpdateCheckCompleted(updateInfo));
            await _messageBus.PublishAsync(new UpdateAvailable(updateInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for updates");
            await _messageBus.PublishAsync(new UpdateCheckCompleted(null));
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DownloadUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (AvailableUpdate is null)
            throw new InvalidOperationException("No update available to download. Call CheckForUpdatesAsync first.");

        if (AvailableUpdate.VelopackUpdate is not Velopack.UpdateInfo veloUpdate)
            throw new InvalidOperationException("Invalid update state.");

        await _messageBus.PublishAsync(new UpdateDownloadStarted(AvailableUpdate));

        try
        {
            var manager = GetUpdateManager();

            await manager.DownloadUpdatesAsync(veloUpdate, progress =>
            {
                // Fire and forget progress updates - they're informational
                _ = _messageBus.PublishAsync(new UpdateDownloadProgress(AvailableUpdate, progress));
            });

            AvailableUpdate.IsDownloaded = true;
            _logger.LogInformation("Update downloaded: {Version}", AvailableUpdate.TargetVersion);

            await _messageBus.PublishAsync(new UpdateDownloadCompleted(AvailableUpdate));
            await _messageBus.PublishAsync(new UpdateReadyToInstall(AvailableUpdate));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download update");
            throw;
        }
    }

    /// <inheritdoc />
    public void ApplyUpdateAndRestart()
    {
        if (AvailableUpdate?.VelopackUpdate is not Velopack.UpdateInfo veloUpdate)
            throw new InvalidOperationException("No downloaded update available to apply.");

        if (!AvailableUpdate.IsDownloaded)
            throw new InvalidOperationException("Update has not been downloaded yet.");

        _logger.LogInformation("Applying update and restarting: {Version}", AvailableUpdate.TargetVersion);
        GetUpdateManager().ApplyUpdatesAndRestart(veloUpdate);
    }

    /// <inheritdoc />
    public void ApplyUpdateOnExit()
    {
        if (AvailableUpdate?.VelopackUpdate is not Velopack.UpdateInfo veloUpdate)
            throw new InvalidOperationException("No downloaded update available to apply.");

        if (!AvailableUpdate.IsDownloaded)
            throw new InvalidOperationException("Update has not been downloaded yet.");

        _logger.LogInformation("Update will be applied on exit: {Version}", AvailableUpdate.TargetVersion);
        GetUpdateManager().ApplyUpdatesAndExit(veloUpdate);
    }
}
