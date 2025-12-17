namespace Mythetech.Components.Infrastructure;

/// <summary>
/// Represents a file filter for file dialogs with a display name and associated extensions
/// </summary>
/// <param name="Name">The display name of the filter (e.g., "Image Files")</param>
/// <param name="Extensions">The file extensions associated with this filter (e.g., ["png", "jpg", "gif"])</param>
public record FileFilter(string Name, string[] Extensions);

