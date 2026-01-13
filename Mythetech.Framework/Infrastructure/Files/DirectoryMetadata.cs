namespace Mythetech.Framework.Infrastructure.Files;

/// <summary>
/// Represents metadata about a directory in the file system.
/// </summary>
/// <param name="Path">The full path to the directory</param>
/// <param name="Name">The directory name</param>
/// <param name="CreatedUtc">When the directory was created (UTC)</param>
/// <param name="ModifiedUtc">When the directory was last modified (UTC)</param>
/// <param name="IsEmpty">Whether the directory contains no files or subdirectories</param>
public record DirectoryMetadata(
    string Path,
    string Name,
    DateTime CreatedUtc,
    DateTime ModifiedUtc,
    bool IsEmpty);
