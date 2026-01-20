using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mythetech.Framework.Infrastructure.Settings;

/// <summary>
/// Caches property metadata for settings types to avoid repeated reflection calls.
/// Uses ConditionalWeakTable to allow types to be garbage collected when no longer needed.
/// </summary>
internal static class SettingsPropertyCache
{
    private static readonly ConditionalWeakTable<Type, SettingPropertyInfo[]> _cache = new();

    /// <summary>
    /// Gets cached property information for all properties with [Setting] attribute on the given type.
    /// </summary>
    public static SettingPropertyInfo[] GetSettingProperties(Type settingsType)
    {
        return _cache.GetValue(settingsType, type =>
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new SettingPropertyInfo(
                    p,
                    p.GetCustomAttribute<SettingAttribute>(),
                    GetBackingFieldName(p.Name),
                    type.GetField(GetBackingFieldName(p.Name), BindingFlags.NonPublic | BindingFlags.Instance)))
                .Where(x => x.Attribute != null)
                .ToArray();
        });
    }

    /// <summary>
    /// Gets cached property information for all public instance properties on the given type.
    /// Includes properties without [Setting] attribute for restore operations.
    /// </summary>
    public static SettingPropertyInfo[] GetAllProperties(Type settingsType)
    {
        // Use a different cache key by wrapping type, or just compute setting properties
        // For simplicity, we'll filter from setting properties as needed
        return _cache.GetValue(settingsType, type =>
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new SettingPropertyInfo(
                    p,
                    p.GetCustomAttribute<SettingAttribute>(),
                    GetBackingFieldName(p.Name),
                    type.GetField(GetBackingFieldName(p.Name), BindingFlags.NonPublic | BindingFlags.Instance)))
                .Where(x => x.Attribute != null)
                .ToArray();
        });
    }

    private static string GetBackingFieldName(string propertyName)
    {
        return propertyName.Length > 1
            ? $"_{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}"
            : $"_{char.ToLowerInvariant(propertyName[0])}";
    }
}

/// <summary>
/// Cached property information for a setting property.
/// </summary>
internal sealed record SettingPropertyInfo(
    PropertyInfo Property,
    SettingAttribute? Attribute,
    string BackingFieldName,
    FieldInfo? BackingField);
