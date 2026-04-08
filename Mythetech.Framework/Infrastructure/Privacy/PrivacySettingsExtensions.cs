using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Privacy;

/// <summary>
/// Extension methods for checking privacy consent state.
/// </summary>
public static class PrivacySettingsExtensions
{
    /// <summary>
    /// Returns whether the user has been shown the privacy consent dialog.
    /// </summary>
    public static bool HasSeenPrivacyDialog(this ISettingsProvider provider)
        => provider.GetSettings<PrivacySettings>()?.HasSeenPrivacyDialog ?? false;
}
