using Mythetech.Framework.Infrastructure.Shell;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Shell;

public class ShellQuotingTests
{
    #region Quote (POSIX) Tests

    [Fact(DisplayName = "Quote_SimpleString_WrapsInSingleQuotes")]
    public void Quote_SimpleString_WrapsInSingleQuotes()
    {
        // Arrange
        var input = "hello";

        // Act
        var result = ShellQuoting.Quote(input);

        // Assert
        result.ShouldBe("'hello'");
    }

    [Fact(DisplayName = "Quote_StringWithSpaces_WrapsInSingleQuotes")]
    public void Quote_StringWithSpaces_WrapsInSingleQuotes()
    {
        // Arrange
        var input = "hello world";

        // Act
        var result = ShellQuoting.Quote(input);

        // Assert
        result.ShouldBe("'hello world'");
    }

    [Fact(DisplayName = "Quote_StringWithSingleQuote_EscapesQuote")]
    public void Quote_StringWithSingleQuote_EscapesQuote()
    {
        // Arrange
        var input = "it's";

        // Act
        var result = ShellQuoting.Quote(input);

        // Assert
        // Expected: 'it'\''s'
        // Breakdown: 'it' + \' + 's'
        result.ShouldBe("'it'\\''s'");
    }

    [Fact(DisplayName = "Quote_StringWithMultipleSingleQuotes_EscapesAll")]
    public void Quote_StringWithMultipleSingleQuotes_EscapesAll()
    {
        // Arrange
        var input = "don't won't";

        // Act
        var result = ShellQuoting.Quote(input);

        // Assert
        result.ShouldBe("'don'\\''t won'\\''t'");
    }

    [Fact(DisplayName = "Quote_EmptyString_ReturnsEmptyQuotes")]
    public void Quote_EmptyString_ReturnsEmptyQuotes()
    {
        // Arrange
        var input = "";

        // Act
        var result = ShellQuoting.Quote(input);

        // Assert
        result.ShouldBe("''");
    }

    [Fact(DisplayName = "Quote_StringWithDollarSign_PreservesLiterally")]
    public void Quote_StringWithDollarSign_PreservesLiterally()
    {
        // Arrange
        var input = "$HOME";

        // Act
        var result = ShellQuoting.Quote(input);

        // Assert
        // Single quotes prevent variable expansion
        result.ShouldBe("'$HOME'");
    }

    [Fact(DisplayName = "Quote_StringWithBackticks_PreservesLiterally")]
    public void Quote_StringWithBackticks_PreservesLiterally()
    {
        // Arrange
        var input = "`whoami`";

        // Act
        var result = ShellQuoting.Quote(input);

        // Assert
        // Single quotes prevent command substitution
        result.ShouldBe("'`whoami`'");
    }

    [Fact(DisplayName = "Quote_FilePath_HandlesSpecialCharacters")]
    public void Quote_FilePath_HandlesSpecialCharacters()
    {
        // Arrange
        var input = "/Users/john/My Documents/file (1).txt";

        // Act
        var result = ShellQuoting.Quote(input);

        // Assert
        result.ShouldBe("'/Users/john/My Documents/file (1).txt'");
    }

    #endregion

    #region QuoteWindows Tests

    [Fact(DisplayName = "QuoteWindows_SimpleString_WrapsInDoubleQuotes")]
    public void QuoteWindows_SimpleString_WrapsInDoubleQuotes()
    {
        // Arrange
        var input = "hello";

        // Act
        var result = ShellQuoting.QuoteWindows(input);

        // Assert
        result.ShouldBe("\"hello\"");
    }

    [Fact(DisplayName = "QuoteWindows_StringWithDoubleQuote_EscapesQuote")]
    public void QuoteWindows_StringWithDoubleQuote_EscapesQuote()
    {
        // Arrange
        var input = "say \"hello\"";

        // Act
        var result = ShellQuoting.QuoteWindows(input);

        // Assert
        result.ShouldBe("\"say \\\"hello\\\"\"");
    }

    [Fact(DisplayName = "QuoteWindows_StringWithBackslash_EscapesBackslash")]
    public void QuoteWindows_StringWithBackslash_EscapesBackslash()
    {
        // Arrange
        var input = @"C:\Users\john";

        // Act
        var result = ShellQuoting.QuoteWindows(input);

        // Assert
        result.ShouldBe("\"C:\\\\Users\\\\john\"");
    }

    [Fact(DisplayName = "QuoteWindows_EmptyString_ReturnsEmptyQuotes")]
    public void QuoteWindows_EmptyString_ReturnsEmptyQuotes()
    {
        // Arrange
        var input = "";

        // Act
        var result = ShellQuoting.QuoteWindows(input);

        // Assert
        result.ShouldBe("\"\"");
    }

    [Fact(DisplayName = "QuoteWindows_StringWithPercent_EscapesPercent")]
    public void QuoteWindows_StringWithPercent_EscapesPercent()
    {
        // Arrange
        var input = "%PATH%";

        // Act
        var result = ShellQuoting.QuoteWindows(input);

        // Assert
        // % must be doubled in cmd.exe
        result.ShouldBe("\"%%PATH%%\"");
    }

    [Fact(DisplayName = "QuoteWindows_StringWithCaret_EscapesCaret")]
    public void QuoteWindows_StringWithCaret_EscapesCaret()
    {
        // Arrange
        var input = "a^b";

        // Act
        var result = ShellQuoting.QuoteWindows(input);

        // Assert
        // ^ is the escape char in cmd.exe, must be doubled
        result.ShouldBe("\"a^^b\"");
    }

    [Fact(DisplayName = "QuoteWindows_StringWithExclamation_EscapesExclamation")]
    public void QuoteWindows_StringWithExclamation_EscapesExclamation()
    {
        // Arrange
        var input = "hello!";

        // Act
        var result = ShellQuoting.QuoteWindows(input);

        // Assert
        // ! is used in delayed expansion, escaped with ^
        result.ShouldBe("\"hello^!\"");
    }

    #endregion

    #region QuoteIfNeeded Tests

    [Fact(DisplayName = "QuoteIfNeeded_SimpleString_ReturnsUnquoted")]
    public void QuoteIfNeeded_SimpleString_ReturnsUnquoted()
    {
        // Arrange
        var input = "hello";

        // Act
        var result = ShellQuoting.QuoteIfNeeded(input);

        // Assert
        result.ShouldBe("hello");
    }

    [Fact(DisplayName = "QuoteIfNeeded_StringWithSpace_ReturnsQuoted")]
    public void QuoteIfNeeded_StringWithSpace_ReturnsQuoted()
    {
        // Arrange
        var input = "hello world";

        // Act
        var result = ShellQuoting.QuoteIfNeeded(input);

        // Assert
        result.ShouldNotBe("hello world");
        result.ShouldContain("hello world");
    }

    [Fact(DisplayName = "QuoteIfNeeded_StringWithPipe_ReturnsQuoted")]
    public void QuoteIfNeeded_StringWithPipe_ReturnsQuoted()
    {
        // Arrange
        var input = "a|b";

        // Act
        var result = ShellQuoting.QuoteIfNeeded(input);

        // Assert
        result.ShouldNotBe("a|b");
    }

    [Fact(DisplayName = "QuoteIfNeeded_StringWithSemicolon_ReturnsQuoted")]
    public void QuoteIfNeeded_StringWithSemicolon_ReturnsQuoted()
    {
        // Arrange
        var input = "cmd1;cmd2";

        // Act
        var result = ShellQuoting.QuoteIfNeeded(input);

        // Assert
        result.ShouldNotBe("cmd1;cmd2");
    }

    [Fact(DisplayName = "QuoteIfNeeded_EmptyString_ReturnsQuoted")]
    public void QuoteIfNeeded_EmptyString_ReturnsQuoted()
    {
        // Arrange
        var input = "";

        // Act
        var result = ShellQuoting.QuoteIfNeeded(input);

        // Assert
        // Empty strings need quoting to be valid arguments
        result.ShouldNotBe("");
    }

    [Fact(DisplayName = "QuoteIfNeeded_StringWithWildcard_ReturnsQuoted")]
    public void QuoteIfNeeded_StringWithWildcard_ReturnsQuoted()
    {
        // Arrange
        var input = "*.txt";

        // Act
        var result = ShellQuoting.QuoteIfNeeded(input);

        // Assert
        result.ShouldNotBe("*.txt");
    }

    [Fact(DisplayName = "QuoteIfNeeded_StringWithPercent_ReturnsQuoted")]
    public void QuoteIfNeeded_StringWithPercent_ReturnsQuoted()
    {
        // Arrange
        var input = "%PATH%";

        // Act
        var result = ShellQuoting.QuoteIfNeeded(input);

        // Assert
        // % needs quoting for Windows env var safety
        result.ShouldNotBe("%PATH%");
    }

    [Fact(DisplayName = "QuoteIfNeeded_StringWithCaret_ReturnsQuoted")]
    public void QuoteIfNeeded_StringWithCaret_ReturnsQuoted()
    {
        // Arrange
        var input = "a^b";

        // Act
        var result = ShellQuoting.QuoteIfNeeded(input);

        // Assert
        // ^ needs quoting for cmd.exe safety
        result.ShouldNotBe("a^b");
    }

    #endregion

    #region Real-World Scenarios

    [Fact(DisplayName = "Quote_GitCommitMessage_HandlesAllCharacters")]
    public void Quote_GitCommitMessage_HandlesAllCharacters()
    {
        // Arrange
        var message = "fix: handle user's \"special\" characters & symbols";

        // Act
        var result = ShellQuoting.Quote(message);

        // Assert
        // Should safely quote for git commit -m
        result.ShouldStartWith("'");
        result.ShouldEndWith("'");
    }

    [Fact(DisplayName = "Quote_BranchName_HandlesSlashes")]
    public void Quote_BranchName_HandlesSlashes()
    {
        // Arrange
        var branchName = "feature/user-auth";

        // Act
        var result = ShellQuoting.Quote(branchName);

        // Assert
        result.ShouldBe("'feature/user-auth'");
    }

    [Fact(DisplayName = "Quote_UnicodeCharacters_PreservesCharacters")]
    public void Quote_UnicodeCharacters_PreservesCharacters()
    {
        // Arrange
        var input = "日本語テスト";

        // Act
        var result = ShellQuoting.Quote(input);

        // Assert
        result.ShouldBe("'日本語テスト'");
    }

    #endregion
}
