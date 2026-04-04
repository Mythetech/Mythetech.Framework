using Mythetech.Framework.AI.Generator.Utilities;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Mcp.Generator;

public class NamingConventionsTests
{
    [Theory]
    [InlineData("BuildSolution", "build_solution")]
    [InlineData("FormatActiveDocument", "format_active_document")]
    [InlineData("FocusFile", "focus_file")]
    [InlineData("GetSymbolInfo", "get_symbol_info")]
    [InlineData("", "")]
    public void ToSnakeCase_converts_correctly(string input, string expected)
    {
        NamingConventions.ToSnakeCase(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("filePath", "FilePath")]
    [InlineData("FilePath", "FilePath")]
    [InlineData("", "")]
    public void ToPascalCase_converts_correctly(string input, string expected)
    {
        NamingConventions.ToPascalCase(input).ShouldBe(expected);
    }

    [Theory]
    [InlineData("FilePath", "filePath")]
    [InlineData("filePath", "filePath")]
    [InlineData("", "")]
    public void ToCamelCase_converts_correctly(string input, string expected)
    {
        NamingConventions.ToCamelCase(input).ShouldBe(expected);
    }
}
