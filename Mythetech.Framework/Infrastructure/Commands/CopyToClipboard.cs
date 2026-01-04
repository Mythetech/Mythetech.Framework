namespace Mythetech.Framework.Infrastructure.Commands;

/// <summary>
/// Command message to copy text to the clipboard.
/// </summary>
/// <param name="Text">The text to copy to the clipboard</param>
public record CopyToClipboard(string Text);
