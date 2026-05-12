namespace Mythetech.Framework.Infrastructure;

/// <summary>
/// Abstract interface for copying text to the host clipboard.
/// </summary>
public interface ICopyToClipboard
{
    /// <summary>
    /// Copies the given text to the system clipboard.
    /// </summary>
    /// <param name="text">The text to copy.</param>
    Task CopyToClipboardAsync(string text);
}
