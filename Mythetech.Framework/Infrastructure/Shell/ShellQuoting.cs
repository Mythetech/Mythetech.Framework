namespace Mythetech.Framework.Infrastructure.Shell;

/// <summary>
/// Cross-platform shell argument quoting utilities.
/// Use these methods to safely include user-supplied values in shell commands.
/// </summary>
public static class ShellQuoting
{
    /// <summary>
    /// Quotes a value for POSIX shells (bash, zsh) using single quotes.
    /// Single quotes prevent all shell interpretation except for the quote itself.
    /// </summary>
    /// <remarks>
    /// Pattern: Replace ' with '\'' (end quote, escaped literal quote, start quote)
    /// Example: "it's here" becomes 'it'\''s here'
    /// </remarks>
    public static string Quote(string value)
        => "'" + value.Replace("'", "'\\''") + "'";

    /// <summary>
    /// Quotes a value for Windows cmd.exe using double quotes.
    /// </summary>
    /// <remarks>
    /// Escapes backslashes, double quotes, and cmd.exe special characters (%, ^, !).
    /// For complex cases, prefer using ProcessStartInfo.ArgumentList.
    /// </remarks>
    public static string QuoteWindows(string value)
    {
        // Escape backslashes and double quotes
        var escaped = value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");

        // Escape cmd.exe special characters
        // % must be doubled to escape in cmd.exe
        // ^ is the escape character, so it must be escaped
        // ! is used in delayed expansion
        escaped = escaped
            .Replace("%", "%%")
            .Replace("^", "^^")
            .Replace("!", "^!");

        return "\"" + escaped + "\"";
    }

    /// <summary>
    /// Platform-appropriate quoting.
    /// Uses <see cref="QuoteWindows"/> on Windows, <see cref="Quote"/> elsewhere.
    /// </summary>
    public static string QuotePlatform(string value)
        => OperatingSystem.IsWindows() ? QuoteWindows(value) : Quote(value);

    /// <summary>
    /// Quotes a value only if it contains characters that need quoting
    /// (whitespace, quotes, or shell metacharacters).
    /// </summary>
    public static string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrEmpty(value))
            return QuotePlatform(value);

        // Check if quoting is needed
        foreach (var c in value)
        {
            if (char.IsWhiteSpace(c) || c == '"' || c == '\'' ||
                c == '$' || c == '`' || c == '\\' || c == '!' ||
                c == '(' || c == ')' || c == '[' || c == ']' ||
                c == '{' || c == '}' || c == '<' || c == '>' ||
                c == '|' || c == '&' || c == ';' || c == '*' ||
                c == '?' || c == '#' || c == '~' || c == '%' ||
                c == '^')
            {
                return QuotePlatform(value);
            }
        }

        return value;
    }
}
