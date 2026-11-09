using Microsoft.Extensions.Logging.Abstractions;
using MudBlazor;
using Mythetech.Framework.Infrastructure.Commands;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginMenuItemInvokerTests
{
    private readonly IMessageBus _bus = Substitute.For<IMessageBus>();
    private readonly PluginContext _context;
    private readonly PluginMenuItemInvoker _invoker;

    public PluginMenuItemInvokerTests()
    {
        _context = new PluginContext { MessageBus = _bus, StateStore = new PluginStateStore(new PluginState()) };
        _invoker = new PluginMenuItemInvoker(_bus, NullLogger<PluginMenuItemInvoker>.Instance);
    }

    [Fact(DisplayName = "InvokeAsync returns false and notifies when the callback throws")]
    public async Task InvokeAsync_CallbackThrows_ReturnsFalseAndNotifies()
    {
        var result = await _invoker.InvokeAsync(_ => throw new InvalidOperationException("boom"), _context, "Notes");

        result.ShouldBeFalse();
        await _bus.Received(1).PublishAsync(Arg.Is<AddNotification>(n =>
            n.Severity == Severity.Error && n.Message == "Notes couldn't complete that action."));
    }

    [Fact(DisplayName = "InvokeAsync uses generic copy when no plugin name is given")]
    public async Task InvokeAsync_NoPluginName_UsesGenericMessage()
    {
        await _invoker.InvokeAsync(_ => throw new InvalidOperationException("boom"), _context);

        await _bus.Received(1).PublishAsync(Arg.Is<AddNotification>(n =>
            n.Message == "A plugin couldn't complete that action."));
    }

    [Fact(DisplayName = "InvokeAsync returns true and does not notify when the callback succeeds")]
    public async Task InvokeAsync_CallbackSucceeds_ReturnsTrue()
    {
        var ran = false;

        var result = await _invoker.InvokeAsync(_ => { ran = true; return Task.CompletedTask; }, _context, "Notes");

        result.ShouldBeTrue();
        ran.ShouldBeTrue();
        await _bus.DidNotReceive().PublishAsync(Arg.Any<AddNotification>());
    }
}
