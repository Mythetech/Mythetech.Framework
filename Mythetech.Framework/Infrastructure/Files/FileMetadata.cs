namespace Mythetech.Framework.Infrastructure.Files;

/// <summary>
/// Represents metadata about a file in the file system.
/// </summary>
/// <param name="Path">The full path to the file</param>
/// <param name="Name">The file name including extension</param>
/// <param name="Extension">The file extension (e.g., ".txt")</param>
/// <param name="Size">The file size in bytes</param>
/// <param name="CreatedUtc">When the file was created (UTC)</param>
/// <param name="ModifiedUtc">When the file was last modified (UTC)</param>
/// <param name="IsReadOnly">Whether the file is read-only</param>
public record FileMetadata(
    string Path,
    string Name,
    string Extension,
    long Size,
    DateTime CreatedUtc,
    DateTime ModifiedUtc,
    bool IsReadOnly);
