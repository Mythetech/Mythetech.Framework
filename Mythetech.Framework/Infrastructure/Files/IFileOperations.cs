namespace Mythetech.Framework.Infrastructure.Files;

/// <summary>
/// Combined interface providing all file operations.
/// Inject this interface when you need full file system access.
/// Implementations are platform-specific (Desktop uses System.IO, WebAssembly throws PlatformNotSupportedException).
/// </summary>
public interface IFileOperations : IFileReader, IFileWriter, IFileManager
{
}
