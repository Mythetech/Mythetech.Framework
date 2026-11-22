using System.Text.RegularExpressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;
using Mythetech.Framework.Components.Input;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Input;

public class MtTextFieldContentTests : BunitContext
{
    public MtTextFieldContentTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "StartContent renders inside the field before the input")]
    public void StartContent_renders_inside_the_field_before_the_input()
    {
        var cut = Render<MtTextField<string>>(parameters => parameters
            .Add(p => p.StartContent, (RenderFragment)(b => b.AddMarkupContent(0, "<i class=\"probe\"></i>"))));

        var slot = cut.Find(".mt-text-field > .mt-tf-start-content");
        slot.QuerySelector(".probe").ShouldNotBeNull();
        slot.NextElementSibling!.ClassList.ShouldContain("mt-tf-input");
        cut.Find(".mt-text-field").ClassList.ShouldContain("mt-tf-has-start-content");
    }

    [Fact(DisplayName = "EndContent renders inside the field after the input")]
    public void EndContent_renders_inside_the_field_after_the_input()
    {
        var cut = Render<MtTextField<string>>(parameters => parameters
            .Add(p => p.EndContent, (RenderFragment)(b => b.AddMarkupContent(0, "<i class=\"probe\"></i>"))));

        var slot = cut.Find(".mt-text-field > .mt-tf-end-content");
        slot.QuerySelector(".probe").ShouldNotBeNull();
        slot.PreviousElementSibling!.ClassList.ShouldContain("mt-tf-input");
        cut.Find(".mt-text-field").ClassList.ShouldContain("mt-tf-has-end-content");
    }

    [Fact(DisplayName = "Without content the field renders as before")]
    public void Without_content_the_field_renders_as_before()
    {
        var plain = Render<MtTextField<string>>(parameters => parameters.Add(p => p.Placeholder, "x"));
        var explicitNull = Render<MtTextField<string>>(parameters => parameters
            .Add(p => p.Placeholder, "x")
            .Add(p => p.StartContent, (RenderFragment?)null)
            .Add(p => p.EndContent, (RenderFragment?)null));

        Normalize(explicitNull.Markup).ShouldBe(Normalize(plain.Markup));
        plain.Markup.ShouldNotContain("mt-tf-start-content");
        plain.Markup.ShouldNotContain("mt-tf-end-content");
        plain.Markup.ShouldNotContain("has-start-content");
    }

    private static string Normalize(string markup) =>
        Regex.Replace(markup, "(blazor:\\w+)=\"[^\"]*\"", "$1");
}
