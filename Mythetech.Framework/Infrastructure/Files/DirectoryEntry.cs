namespace Mythetech.Framework.Infrastructure.Files;

/// <summary>
/// Represents an entry in a directory listing.
/// </summary>
/// <param name="Path">The full path to the entry</param>
/// <param name="Name">The name of the file or directory</param>
/// <param name="IsDirectory">True if this is a directory, false if a file</param>
public record DirectoryEntry(
    string Path,
    string Name,
    bool IsDirectory);
