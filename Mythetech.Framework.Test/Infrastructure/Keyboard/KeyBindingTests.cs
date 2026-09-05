using MudBlazor.Utilities;
using Mythetech.Framework.Infrastructure.Keyboard;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Keyboard;

public class KeyBindingTests
{
    [Fact(DisplayName = "CtrlOrCmd factory sets the accelerator modifier")]
    public void CtrlOrCmd_sets_accelerator()
    {
        var binding = KeyBinding.CtrlOrCmd(JsKey.Enter);

        binding.Key.ShouldBe(JsKey.Enter);
        binding.Ctrl.ShouldBeTrue();
        binding.Shift.ShouldBeFalse();
        binding.Alt.ShouldBeFalse();
    }

    [Fact(DisplayName = "Tokens render the accelerator as Cmd so MtKbd can map it per platform")]
    public void Tokens_use_cmd_for_accelerator()
    {
        var binding = KeyBinding.CtrlOrCmd(JsKey.KeyK, shift: true);

        binding.ToTokens().ShouldBe(new[] { "Cmd", "Shift", "K" });
    }

    [Theory(DisplayName = "Tokens strip the DOM code prefix from letter and digit keys")]
    [InlineData(JsKey.KeyB, "B")]
    [InlineData(JsKey.Digit1, "1")]
    [InlineData(JsKey.F12, "F12")]
    [InlineData(JsKey.Enter, "Enter")]
    [InlineData(JsKey.Backquote, "`")]
    [InlineData(JsKey.Backslash, "\\")]
    public void Tokens_map_key_names(JsKey key, string expected)
    {
        KeyBinding.Plain(key).ToTokens().ShouldBe(new[] { expected });
    }

    [Fact(DisplayName = "Serialization round trips a binding")]
    public void Serialization_round_trips()
    {
        var binding = new KeyBinding(JsKey.KeyS, Ctrl: true, Shift: true, Alt: true);

        var restored = SerializedKeyBinding.FromBinding(binding).ToBinding();

        restored.ShouldBe(binding);
    }

    [Fact(DisplayName = "Serialization stores the DOM code name rather than the enum ordinal")]
    public void Serialization_stores_code_name()
    {
        SerializedKeyBinding.FromBinding(KeyBinding.Plain(JsKey.KeyS)).Key.ShouldBe("KeyS");
    }

    [Fact(DisplayName = "An unrecognized code name resolves to unbound instead of throwing")]
    public void Unknown_code_name_resolves_to_null()
    {
        new SerializedKeyBinding("KeyThatDoesNotExist", false, false, false)
            .ToBinding()
            .ShouldBeNull();
    }

    [Fact(DisplayName = "A numeric string that TryParse accepts but is not a defined enum member resolves to null")]
    public void Numeric_string_out_of_range_resolves_to_null()
    {
        new SerializedKeyBinding("999", false, false, false)
            .ToBinding()
            .ShouldBeNull();
    }
}
