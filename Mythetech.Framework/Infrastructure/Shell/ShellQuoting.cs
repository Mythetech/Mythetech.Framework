using System.Text;

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
    /// Quotes a value as a single Windows command-line argument, following the
    /// CreateProcess / CommandLineToArgvW parsing rules used by the child process.
    /// </summary>
    /// <remarks>
    /// Wraps the value in double quotes, escapes embedded quotes as \", and doubles
    /// only the backslashes that come directly before a quote or the closing quote.
    /// cmd.exe characters such as %, ^ and ! are left alone because the executor
    /// never routes arguments through cmd.exe. Batch files (.cmd, .bat) are the
    /// exception: Windows runs them through cmd.exe, and this quoting is not safe there.
    /// Prefer <see cref="ShellCommand.ArgumentList"/>, which needs no quoting at all.
    /// </remarks>
    public static string QuoteWindows(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');

        var pendingBackslashes = 0;
        foreach (var c in value)
        {
            if (c == '\\')
            {
                pendingBackslashes++;
                continue;
            }

            if (c == '"')
            {
                builder.Append('\\', pendingBackslashes * 2 + 1);
            }
            else
            {
                builder.Append('\\', pendingBackslashes);
            }

            builder.Append(c);
            pendingBackslashes = 0;
        }

        builder.Append('\\', pendingBackslashes * 2);
        builder.Append('"');
        return builder.ToString();
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
