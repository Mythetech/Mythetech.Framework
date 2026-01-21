using System.Reflection;
using System.Text.RegularExpressions;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Settings;

namespace Mythetech.Framework.Infrastructure.FeatureFlags;

/// <summary>
/// Base class for application-specific feature flag definitions.
/// Inherit from this class and define bool properties with [FeatureFlag] attribute.
/// </summary>
/// <example>
/// <code>
/// public class MyAppFeatureFlags : FeatureFlagsSettingsBase
/// {
///     public override string SettingsId => "MyAppFeatureFlags";
///     public override string DisplayName => "Feature Flags";
///
///     [FeatureFlag(Label = "Dark Mode", Description = "Enable dark mode theme")]
///     public bool DarkMode { get; set; } = false;
///
///     [FeatureFlag(Label = "Beta Features", Group = "Experimental")]
///     public bool BetaFeatures { get; set; } = false;
/// }
/// </code>
/// </example>
public abstract class FeatureFlagsSettingsBase : SettingsBase
{
    /// <summary>
    /// Default icon for feature flag settings sections.
    /// </summary>
    public override string Icon => Icons.Material.Filled.Flag;

    /// <summary>
    /// Feature flags appear after MCP settings in the settings UI.
    /// </summary>
    public override int Order => 400;

    /// <summary>
    /// Gets all feature flag metadata from this instance.
    /// </summary>
    public IEnumerable<FeatureFlagInfo> GetFeatureFlags()
    {
        var type = GetType();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.PropertyType != typeof(bool))
                continue;

            var attr = property.GetCustomAttribute<FeatureFlagAttribute>();
            if (attr == null)
                continue;

            yield return new FeatureFlagInfo
            {
                Key = attr.Key ?? property.Name,
                Label = attr.Label ?? SplitCamelCase(property.Name),
                Description = attr.Description,
                Group = attr.Group,
                Order = attr.Order,
                Property = property,
                SourceSettings = this
            };
        }
    }

    /// <summary>
    /// Gets the current value of a flag by key.
    /// </summary>
    public bool GetFlagValue(string key)
    {
        var flag = GetFeatureFlags().FirstOrDefault(f => f.Key == key);
        if (flag == null)
            return false;

        return (bool)(flag.Property.GetValue(this) ?? false);
    }

    /// <summary>
    /// Sets the value of a flag by key.
    /// </summary>
    public void SetFlagValue(string key, bool value)
    {
        var flag = GetFeatureFlags().FirstOrDefault(f => f.Key == key);
        flag?.Property.SetValue(this, value);
    }

    private static string SplitCamelCase(string input)
    {
        return Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");
    }
}
