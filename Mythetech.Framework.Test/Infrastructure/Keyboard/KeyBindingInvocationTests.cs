using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MudBlazor.Utilities;
using Mythetech.Framework.Infrastructure.Keyboard;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Settings;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Keyboard;

public class KeyBindingInvocationTests
{
    private readonly ISettingsProvider _settingsProvider = Substitute.For<ISettingsProvider>();
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly KeyBindingSettings _settings = new(Options.Create(new KeyBindingSettingsOptions()));

    private KeyBindingService CreateService(params KeyBindingDefinition[] definitions)
    {
        var options = new KeyBindingRegistrationOptions();
        options.Definitions.AddRange(definitions);

        return new KeyBindingService(
            new KeyBindingRegistry(Options.Create(options)),
            _settings,
            _settingsProvider,
            _bus,
            NullLogger<KeyBindingService>.Instance);
    }

    [Fact(DisplayName = "Invoking an action runs its handler with the message bus")]
    public async Task Invokes_handler_with_bus()
    {
        IMessageBus? received = null;
        var definition = new KeyBindingDefinition(
            "save", "Save", null, "Edit", KeyBinding.CtrlOrCmd(JsKey.KeyS),
            (bus, ct) =>
            {
                received = bus;
                return Task.CompletedTask;
            });

        var service = CreateService(definition);

        await service.InvokeAsync("save", CancellationToken.None);

        received.ShouldBeSameAs(_bus);
    }

    [Fact(DisplayName = "Invoking an action passes the cancellation token through")]
    public async Task Passes_cancellation_token()
    {
        using var cts = new CancellationTokenSource();
        var received = CancellationToken.None;
        var definition = new KeyBindingDefinition(
            "save", "Save", null, "Edit", null,
            (bus, ct) =>
            {
                received = ct;
                return Task.CompletedTask;
            });

        var service = CreateService(definition);

        await service.InvokeAsync("save", cts.Token);

        received.ShouldBe(cts.Token);
    }

    [Fact(DisplayName = "Invoking an unknown id is a no-op rather than a throw")]
    public async Task Unknown_id_does_not_throw()
    {
        var service = CreateService();

        await Should.NotThrowAsync(() => service.InvokeAsync("missing", CancellationToken.None));
    }

    [Fact(DisplayName = "A throwing handler is contained so it cannot escape into the JS interop boundary")]
    public async Task Throwing_handler_does_not_propagate()
    {
        var definition = new KeyBindingDefinition(
            "save", "Save", null, "Edit", KeyBinding.CtrlOrCmd(JsKey.KeyS),
            (bus, ct) => throw new InvalidOperationException("handler blew up"));

        var service = CreateService(definition);

        await Should.NotThrowAsync(() => service.InvokeAsync("save", CancellationToken.None));
    }

    [Fact(DisplayName = "A handler that faults its task is contained the same way as one that throws synchronously")]
    public async Task Faulted_handler_task_does_not_propagate()
    {
        var definition = new KeyBindingDefinition(
            "save", "Save", null, "Edit", KeyBinding.CtrlOrCmd(JsKey.KeyS),
            (bus, ct) => Task.FromException(new InvalidOperationException("handler blew up")));

        var service = CreateService(definition);

        await Should.NotThrowAsync(() => service.InvokeAsync("save", CancellationToken.None));
    }

    [Fact(DisplayName = "A throwing handler does not stop a later invocation from running")]
    public async Task Throwing_handler_does_not_break_later_invocations()
    {
        var ran = false;
        var service = CreateService(
            new KeyBindingDefinition(
                "bad", "Bad", null, "Edit", null,
                (bus, ct) => throw new InvalidOperationException("handler blew up")),
            new KeyBindingDefinition(
                "good", "Good", null, "Edit", null,
                (bus, ct) =>
                {
                    ran = true;
                    return Task.CompletedTask;
                }));

        await service.InvokeAsync("bad", CancellationToken.None);
        await service.InvokeAsync("good", CancellationToken.None);

        ran.ShouldBeTrue();
    }
}
