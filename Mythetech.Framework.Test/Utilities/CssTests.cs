using Mythetech.Framework.Utilities;
using Shouldly;

namespace Mythetech.Framework.Test.Utilities;

public class CssTests
{
    [Fact]
    public void Merge_EmptyArgs_ReturnsEmpty()
    {
        Css.Merge().ShouldBe(string.Empty);
    }

    [Fact]
    public void Merge_SingleClass_ReturnsTrimmed()
    {
        Css.Merge("  foo  ").ShouldBe("foo");
    }

    [Fact]
    public void Merge_MultipleStrings_Joins()
    {
        Css.Merge("foo", "bar").ShouldBe("foo bar");
    }

    [Fact]
    public void Merge_Deduplicates()
    {
        Css.Merge("foo bar", "bar baz").ShouldBe("foo bar baz");
    }

    [Fact]
    public void Merge_SkipsNullAndEmpty()
    {
        Css.Merge("foo", null, "", "  ", "bar").ShouldBe("foo bar");
    }

    [Fact]
    public void Merge_NormalizesExtraSpaces()
    {
        Css.Merge("  foo   bar  ").ShouldBe("foo bar");
    }

    [Fact]
    public void Merge_IdenticalStringsDeduped()
    {
        Css.Merge("context-item-active", "context-item-active").ShouldBe("context-item-active");
    }

    [Fact]
    public void MergeIf_IncludesWhenTrue()
    {
        Css.MergeIf(
            ("foo", true),
            ("bar", false),
            ("baz", true)
        ).ShouldBe("foo baz");
    }

    [Fact]
    public void MergeIf_AllFalse_ReturnsEmpty()
    {
        Css.MergeIf(
            ("foo", false),
            ("bar", false)
        ).ShouldBe(string.Empty);
    }

    [Fact]
    public void MergeIf_DeduplicatesAcrossEntries()
    {
        Css.MergeIf(
            ("foo bar", true),
            ("bar baz", true)
        ).ShouldBe("foo bar baz");
    }

    [Fact]
    public void Merge_PreservesClassOrder()
    {
        Css.Merge("c b a").ShouldBe("c b a");
    }
}
