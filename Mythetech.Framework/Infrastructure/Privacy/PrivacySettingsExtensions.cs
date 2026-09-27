using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Privacy;

/// <summary>
/// Extension methods for checking privacy consent state.
/// </summary>
public static class PrivacySettingsExtensions
{
    /// <summary>
    /// Returns whether the user has been shown the privacy consent dialog. Always true in a smoke run, so
    /// apps that ask this before showing the dialog never show it there.
    /// </summary>
    public static bool HasSeenPrivacyDialog(this ISettingsProvider provider)
        // Reported rather than stored: a smoke run must not stop at a consent prompt, and must not record a
        // consent decision in a real settings store either.
        => provider is SettingsProvider { IsSmokeRun: true }
           || (provider.GetSettings<PrivacySettings>()?.HasSeenPrivacyDialog ?? false);
}
