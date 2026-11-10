using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;
using Mythetech.Framework.Components.Input;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Input;

public class MtTextFieldTests : BunitContext
{
    public MtTextFieldTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "A parent render that repeats the old Value keeps the text the user typed")]
    public async Task ParentRender_WithUnchangedValue_KeepsTypedText()
    {
        var cut = Render<MtTextField<string>>(parameters => parameters
            .Add(p => p.Value, "a")
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, _ => { })));

        await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "ab" });
        cut.Render(parameters => parameters.Add(p => p.Value, "a"));

        cut.Find("input").GetAttribute("value").ShouldBe("ab");
    }

    [Fact(DisplayName = "A new Value from the parent replaces the text")]
    public async Task ParentRender_WithNewValue_ReplacesText()
    {
        var cut = Render<MtTextField<string>>(parameters => parameters
            .Add(p => p.Value, "a")
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<string>(this, _ => { })));

        await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "ab" });
        cut.Render(parameters => parameters.Add(p => p.Value, "reset"));

        cut.Find("input").GetAttribute("value").ShouldBe("reset");
    }

    [Fact(DisplayName = "The parent echoing the value the field sent keeps the text as typed")]
    public async Task ParentEchoesSentValue_KeepsTextAsTyped()
    {
        var cut = Render<MtTextField<double>>(parameters => parameters
            .Add(p => p.Value, 0d)
            .Add(p => p.ValueChanged, EventCallback.Factory.Create<double>(this, _ => { })));

        await cut.Find("input").InputAsync(new ChangeEventArgs { Value = "1." });
        cut.Render(parameters => parameters.Add(p => p.Value, 1d));

        cut.Find("input").GetAttribute("value").ShouldBe("1.");
    }
}
