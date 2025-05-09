namespace Mythetech.Components.Infrastructure;

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
    public Task<bool> SaveFileAsync(string fileName, string data);
    
    /// <summary>
    /// Prompts behavior to save a file
    /// </summary>
    /// <param name="fileName">Name of the file</param>
    public Task<string?> PromptFileSaveAsync(string fileName);
}