namespace Mythetech.Framework.Infrastructure.Settings;

/// <summary>
/// Represents a search match for a setting.
/// </summary>
/// <param name="Settings">The settings model containing the match.</param>
/// <param name="PropertyName">The name of the matching property.</param>
/// <param name="Attribute">The setting attribute with metadata.</param>
/// <param name="MatchField">Which field matched: "Label", "Description", "Group", or "SectionName".</param>
public record SettingsSearchResult(
    SettingsBase Settings,
    string PropertyName,
    SettingAttribute Attribute,
    string MatchField
);
