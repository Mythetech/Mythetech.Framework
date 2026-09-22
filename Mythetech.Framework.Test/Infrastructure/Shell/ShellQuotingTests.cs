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
        ShellQuoting.QuoteWindows("hello").ShouldBe("\"hello\"");
    }

    [Fact(DisplayName = "QuoteWindows_StringWithDoubleQuote_EscapesQuote")]
    public void QuoteWindows_StringWithDoubleQuote_EscapesQuote()
    {
        ShellQuoting.QuoteWindows("say \"hello\"").ShouldBe("\"say \\\"hello\\\"\"");
    }

    [Fact(DisplayName = "QuoteWindows_BackslashesNotBeforeQuote_LeftAlone")]
    public void QuoteWindows_BackslashesNotBeforeQuote_LeftAlone()
    {
        ShellQuoting.QuoteWindows(@"C:\Users\john").ShouldBe("\"C:\\Users\\john\"");
    }

    [Fact(DisplayName = "QuoteWindows_TrailingBackslash_DoubledBeforeClosingQuote")]
    public void QuoteWindows_TrailingBackslash_DoubledBeforeClosingQuote()
    {
        ShellQuoting.QuoteWindows(@"C:\dir\").ShouldBe("\"C:\\dir\\\\\"");
    }

    [Fact(DisplayName = "QuoteWindows_BackslashBeforeQuote_DoubledAndQuoteEscaped")]
    public void QuoteWindows_BackslashBeforeQuote_DoubledAndQuoteEscaped()
    {
        ShellQuoting.QuoteWindows("a\\\"b").ShouldBe("\"a\\\\\\\"b\"");
    }

    [Fact(DisplayName = "QuoteWindows_EmptyString_ReturnsEmptyQuotes")]
    public void QuoteWindows_EmptyString_ReturnsEmptyQuotes()
    {
        ShellQuoting.QuoteWindows("").ShouldBe("\"\"");
    }

    [Theory(DisplayName = "QuoteWindows_CmdMetacharacters_LeftAlone")]
    [InlineData("100%")]
    [InlineData("%PATH%")]
    [InlineData("a^b")]
    [InlineData("hi!")]
    public void QuoteWindows_CmdMetacharacters_LeftAlone(string input)
    {
        // CreateProcess never involves cmd.exe, so these reach the child verbatim
        ShellQuoting.QuoteWindows(input).ShouldBe("\"" + input + "\"");
    }

    [Theory(DisplayName = "QuoteWindows_RoundTripsThroughCommandLineToArgvW")]
    [InlineData("hello")]
    [InlineData("")]
    [InlineData("hello world")]
    [InlineData("100%")]
    [InlineData("a^b")]
    [InlineData("hi!")]
    [InlineData(@"C:\Users\tom")]
    [InlineData(@"C:\Program Files\dir\")]
    [InlineData(@"C:\dir\\")]
    [InlineData("\\")]
    [InlineData("\\\\")]
    [InlineData("a\\\"b")]
    [InlineData("a\\\\\"b")]
    [InlineData("\"")]
    [InlineData("\"\"")]
    [InlineData("say \"hello\" to \"them\"")]
    [InlineData("tab\there")]
    [InlineData("  leading and trailing  ")]
    [InlineData("{\"mcpServers\":{\"fs\":{\"command\":\"C:\\\\tools\\\\fs.exe\",\"args\":[\"C:\\\\\"]}}}")]
    [InlineData("日本語テスト")]
    [InlineData("emoji 🚀 and ümlaut")]
    [InlineData("& | < > ( ) ;")]
    public void QuoteWindows_RoundTripsThroughCommandLineToArgvW(string input)
    {
        WindowsCommandLine.Parse(ShellQuoting.QuoteWindows(input)).ShouldBe([input]);
    }

    [Fact(DisplayName = "QuoteWindows_MultipleArguments_RoundTripAsSeparateValues")]
    public void QuoteWindows_MultipleArguments_RoundTripAsSeparateValues()
    {
        string[] values = [@"C:\dir\", "--flag", "a \"quoted\" value", "", "50%!"];

        var commandLine = string.Join(" ", values.Select(ShellQuoting.QuoteWindows));

        WindowsCommandLine.Parse(commandLine).ShouldBe(values);
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
