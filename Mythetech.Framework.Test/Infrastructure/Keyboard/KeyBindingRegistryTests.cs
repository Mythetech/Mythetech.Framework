using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor.Utilities;
using Mythetech.Framework.Infrastructure.Keyboard;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Keyboard;

public class KeyBindingRegistryTests
{
    private static KeyBindingRegistry CreateRegistry(params KeyBindingDefinition[] definitions)
    {
        var options = new KeyBindingRegistrationOptions();
        options.Definitions.AddRange(definitions);
        return new KeyBindingRegistry(Options.Create(options));
    }

    private static KeyBindingDefinition Definition(string id, KeyBinding? binding = null, string category = "General")
        => new(id, id, null, category, binding, (_, _) => Task.CompletedTask);

    [Fact(DisplayName = "Registry returns every registered definition")]
    public void Returns_all_definitions()
    {
        var registry = CreateRegistry(Definition("a"), Definition("b"));

        registry.GetAll().Select(d => d.Id).ShouldBe(new[] { "a", "b" });
    }

    [Fact(DisplayName = "Registry looks a definition up by id and returns null for unknown ids")]
    public void Looks_up_by_id()
    {
        var registry = CreateRegistry(Definition("a"));

        registry.Get("a").ShouldNotBeNull();
        registry.Get("missing").ShouldBeNull();
    }

    [Fact(DisplayName = "Duplicate ids throw because ids are persistence keys")]
    public void Duplicate_ids_throw()
    {
        var act = () => CreateRegistry(Definition("a"), Definition("a"));

        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldContain("a");
    }

    [Fact(DisplayName = "Builder applies the current category to subsequent additions")]
    public void Builder_applies_category()
    {
        var services = new ServiceCollection();

        services.AddKeyBindings(b => b
            .Category("Requests")
                .Add("request.run", "Run request", KeyBinding.CtrlOrCmd(JsKey.Enter),
                    (bus, ct) => Task.CompletedTask)
            .Category("Panels")
                .Add("panel.history", "History", KeyBinding.CtrlOrCmd(JsKey.KeyH, shift: true),
                    (bus, ct) => Task.CompletedTask));

        var registry = services.BuildServiceProvider().GetRequiredService<IKeyBindingRegistry>();

        registry.Get("request.run")!.Category.ShouldBe("Requests");
        registry.Get("panel.history")!.Category.ShouldBe("Panels");
    }

    [Fact(DisplayName = "An action can be declared with no default binding")]
    public void Allows_null_default_binding()
    {
        var services = new ServiceCollection();

        services.AddKeyBindings(b => b
            .Add("request.duplicate", "Duplicate request", defaultBinding: null,
                (bus, ct) => Task.CompletedTask));

        var registry = services.BuildServiceProvider().GetRequiredService<IKeyBindingRegistry>();

        registry.Get("request.duplicate")!.DefaultBinding.ShouldBeNull();
    }

    [Fact(DisplayName = "Multiple AddKeyBindings calls accumulate instead of replacing")]
    public void Multiple_registrations_accumulate()
    {
        var services = new ServiceCollection();

        services.AddKeyBindings(b => b.Add("a", "A", null, (bus, ct) => Task.CompletedTask));
        services.AddKeyBindings(b => b.Add("b", "B", null, (bus, ct) => Task.CompletedTask));

        var registry = services.BuildServiceProvider().GetRequiredService<IKeyBindingRegistry>();

        registry.GetAll().Count.ShouldBe(2);
    }
}
