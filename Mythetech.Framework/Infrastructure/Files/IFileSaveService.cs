namespace Mythetech.Framework.Infrastructure.Files;

/// <summary>
/// An abstract way to save files for components
/// </summary>
public interface IFileSaveService
{
    /// <summary>
    /// Saves a file
    /// </summary>
    /// <param name="fileName">Name of the File</param>
    /// <param name="data">Contents of the file</param>
    /// <returns>True once the file is saved, false if the user cancelled</returns>
    public Task<bool> SaveFileAsync(string fileName, string data);

    /// <summary>
    /// Saves a file with binary contents, such as a spreadsheet or an archive
    /// </summary>
    /// <param name="fileName">Name of the file, including its extension</param>
    /// <param name="data">Contents of the file</param>
    /// <returns>True once the file is saved, false if the user cancelled</returns>
    public Task<bool> SaveFileAsync(string fileName, byte[] data);

    /// <summary>
    /// Prompts behavior to save a file
    /// </summary>
    /// <remarks>
    /// On WebAssembly the result is only the chosen file's name, not a path the app can write to.
    /// Use <see cref="SaveFileAsync(string, byte[])"/> to write content on every platform.
    /// </remarks>
    /// <param name="fileName">Name of the file</param>
    /// <param name="extension">Extension to show by default in the prompt</param>
    public Task<string?> PromptFileSaveAsync(string fileName, string extension = "txt");
}
