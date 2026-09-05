using Microsoft.Extensions.Options;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.Keyboard;

/// <summary>
/// Persists the user's shortcut overrides. A present value is the user's
/// binding, a null value means the action was explicitly unbound, and a
/// missing key means the action uses its declared default.
/// </summary>
public class KeyBindingSettings : SettingsBase
{
    private readonly KeyBindingSettingsOptions _options;

    /// <inheritdoc cref="KeyBindingSettings"/>
    public KeyBindingSettings(IOptions<KeyBindingSettingsOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public override string SettingsId => "KeyBindings";

    /// <inheritdoc />
    public override string DisplayName => _options.DisplayName;

    /// <inheritdoc />
    public override string Icon => _options.Icon;

    /// <inheritdoc />
    public override int Order => _options.Order;

    /// <inheritdoc />
    public override Type? BeginningContent => typeof(Components.Kbd.MtKeyBindingsEditor);

    /// <summary>
    /// User overrides, keyed by binding id. Marked as a hidden setting so the
    /// persistence path serializes it while the settings panel renders the
    /// shortcuts editor instead of a raw dictionary field.
    /// </summary>
    [Setting(Hide = true)]
    public Dictionary<string, SerializedKeyBinding?> Overrides { get; set; } = new();
}
