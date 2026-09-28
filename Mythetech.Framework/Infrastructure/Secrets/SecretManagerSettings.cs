using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Secrets;

/// <summary>
/// Remembers which secret manager is active so the choice survives restarts.
/// <see cref="SecretManagerState"/> reads and writes it, and the Secret Manager dialog is where users change it,
/// so it is not shown in the settings panel.
/// </summary>
public class SecretManagerSettings : SettingsBase
{
    /// <inheritdoc />
    public override string SettingsId => "SecretManager";

    /// <inheritdoc />
    public override string DisplayName => "Secret Manager";

    /// <inheritdoc />
    public override string Icon => MythetechFrameworkIcons.Key;

    /// <inheritdoc />
    public override bool ShowInPanel => false;

    /// <summary>
    /// <see cref="ISecretManager.Name"/> of the manager the user chose, or null before a choice is made.
    /// </summary>
    [Setting(Hide = true)]
    public string? ActiveManagerName { get; set; }
}
