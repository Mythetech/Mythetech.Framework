namespace Mythetech.Framework.Infrastructure.Settings;

/// <summary>
/// Implemented by settings storages whose methods log and swallow failures so the app keeps running. The
/// probe opens the store and reads from it, and throws instead, so a smoke run can see a broken store.
/// </summary>
internal interface ISettingsStorageProbe
{
    Task ProbeAsync(CancellationToken cancellationToken);
}
