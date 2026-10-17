using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
    // Flag metadata depends only on the type, and flag lookups run on every render of a feature flag component.
    private static readonly ConditionalWeakTable<Type, FlagDefinitions> DefinitionsByType = new();

    /// <summary>
    /// Default icon for feature flag settings sections.
    /// </summary>
    public override string Icon => MythetechFrameworkIcons.Flag;

    /// <summary>
    /// Feature flags appear after MCP settings in the settings UI.
    /// </summary>
    public override int Order => 400;

    /// <summary>
    /// Gets all feature flag metadata from this instance.
    /// </summary>
    public IEnumerable<FeatureFlagInfo> GetFeatureFlags()
        => Definitions.All.Select(definition => new FeatureFlagInfo
        {
            Key = definition.Key,
            Label = definition.Label,
            Description = definition.Description,
            Group = definition.Group,
            Order = definition.Order,
            Property = definition.Property,
            SourceSettings = this
        });

    /// <summary>
    /// Gets the current value of a flag by key.
    /// </summary>
    public bool GetFlagValue(string key)
        => Definitions.PropertiesByKey.TryGetValue(key, out var property) && (bool)(property.GetValue(this) ?? false);

    /// <summary>
    /// Sets the value of a flag by key.
    /// </summary>
    public void SetFlagValue(string key, bool value)
    {
        if (Definitions.PropertiesByKey.TryGetValue(key, out var property))
            property.SetValue(this, value);
    }

    private FlagDefinitions Definitions => DefinitionsByType.GetValue(GetType(), FlagDefinitions.For);

    private static string SplitCamelCase(string input)
    {
        return Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");
    }

    private sealed record FlagDefinition(string Key, string Label, string? Description, string Group, int Order, PropertyInfo Property);

    private sealed class FlagDefinitions
    {
        public required FlagDefinition[] All { get; init; }

        public required Dictionary<string, PropertyInfo> PropertiesByKey { get; init; }

        public static FlagDefinitions For(Type type)
        {
            var all = new List<FlagDefinition>();
            var propertiesByKey = new Dictionary<string, PropertyInfo>();

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.PropertyType != typeof(bool))
                    continue;

                var attr = property.GetCustomAttribute<FeatureFlagAttribute>();
                if (attr == null)
                    continue;

                var key = attr.Key ?? property.Name;
                all.Add(new FlagDefinition(key, attr.Label ?? SplitCamelCase(property.Name), attr.Description, attr.Group, attr.Order, property));
                propertiesByKey.TryAdd(key, property);
            }

            return new FlagDefinitions { All = [.. all], PropertiesByKey = propertiesByKey };
        }
    }
}
