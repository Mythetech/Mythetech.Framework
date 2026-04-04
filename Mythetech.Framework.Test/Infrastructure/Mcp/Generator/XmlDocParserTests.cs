using Mythetech.Framework.AI.Generator.Utilities;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Mcp.Generator;

public class XmlDocParserTests
{
    [Fact]
    public void Parse_extracts_summary()
    {
        var xml = "<summary>Focus a file in the editor</summary>";
        var result = XmlDocParser.Parse(xml);
        result.Summary.ShouldBe("Focus a file in the editor");
    }

    [Fact]
    public void Parse_extracts_param_descriptions()
    {
        var xml = """
            <summary>Get symbol info</summary>
            <param name="filePath">The file to analyze</param>
            <param name="line">The line number</param>
            """;
        var result = XmlDocParser.Parse(xml);
        result.GetParamDescription("filePath").ShouldBe("The file to analyze");
        result.GetParamDescription("line").ShouldBe("The line number");
    }

    [Fact]
    public void Parse_returns_empty_for_null_input()
    {
        var result = XmlDocParser.Parse(null);
        result.Summary.ShouldBeNull();
    }

    [Fact]
    public void GetParamDescription_returns_fallback_for_unknown_param()
    {
        var result = XmlDocParser.Parse("<summary>Test</summary>");
        result.GetParamDescription("unknown").ShouldBe("The unknown parameter");
    }
}
