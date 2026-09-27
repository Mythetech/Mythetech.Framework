using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mythetech.Framework.Infrastructure.Initialization;
using Mythetech.Framework.Infrastructure.MessageBus;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Initialization;

public class AsyncInitializationHostTests
{
    private static AsyncInitializationHost CreateHost(IMessageBus? bus, params IAsyncInitializationHook[] hooks)
        => new(hooks, NullLogger<AsyncInitializationHost>.Instance, bus);

    [Fact(DisplayName = "Records each hook's outcome in the order the hooks ran")]
    public async Task Records_Each_Hook_Outcome_In_Order()
    {
        var failure = new InvalidOperationException("boom");
        var host = CreateHost(null,
            new TestHook("Late", 300),
            new TestHook("Failing", 200, failure),
            new TestHook("Early", 100));

        await host.InitializeAsync(TestContext.Current.CancellationToken);

        host.Results.Select(r => r.Name).ShouldBe(["Early", "Failing", "Late"]);
        host.Results.Select(r => r.Order).ShouldBe([100, 200, 300]);
        host.Results[0].Succeeded.ShouldBeTrue();
        host.Results[1].Succeeded.ShouldBeFalse();
        host.Results[1].Error.ShouldBeSameAs(failure);
        host.Results[2].Succeeded.ShouldBeTrue();
    }

    [Fact(DisplayName = "IsInitialized stays false until every hook has finished")]
    public async Task IsInitialized_Is_False_Until_The_Run_Finishes()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = CreateHost(null, new TestHook("Blocking", 100, waitFor: release.Task));

        var run = host.InitializeAsync(TestContext.Current.CancellationToken);

        host.IsInitialized.ShouldBeFalse();
        release.SetResult();
        await run;
        host.IsInitialized.ShouldBeTrue();
    }

    [Fact(DisplayName = "Publishes ApplicationReady with the hook results when the run finishes")]
    public async Task Publishes_ApplicationReady_With_Results()
    {
        var bus = Substitute.For<IMessageBus>();
        var host = CreateHost(bus, new TestHook("Settings", 100), new TestHook("Failing", 200, new InvalidOperationException()));

        await host.InitializeAsync(TestContext.Current.CancellationToken);

        await bus.Received(1).PublishAsync(Arg.Is<ApplicationReady>(ready =>
            ready.Hooks.Count == 2 && ready.Hooks[0].Name == "Settings" && !ready.Hooks[1].Succeeded));
    }

    [Fact(DisplayName = "ApplicationReady is published after IsInitialized becomes true")]
    public async Task ApplicationReady_Is_Published_After_Initialization_Completes()
    {
        var bus = Substitute.For<IMessageBus>();
        bool? initializedWhenPublished = null;
        AsyncInitializationHost? host = null;
        bus.PublishAsync(Arg.Any<ApplicationReady>()).Returns(_ =>
        {
            initializedWhenPublished = host!.IsInitialized;
            return Task.CompletedTask;
        });
        host = CreateHost(bus, new TestHook("Settings", 100));

        await host.InitializeAsync(TestContext.Current.CancellationToken);

        initializedWhenPublished.ShouldBe(true);
    }

    [Fact(DisplayName = "A second call runs no hooks and publishes nothing")]
    public async Task Second_Call_Is_A_NoOp()
    {
        var bus = Substitute.For<IMessageBus>();
        var hook = new TestHook("Settings", 100);
        var host = CreateHost(bus, hook);

        await host.InitializeAsync(TestContext.Current.CancellationToken);
        await host.InitializeAsync(TestContext.Current.CancellationToken);

        hook.Runs.ShouldBe(1);
        await bus.Received(1).PublishAsync(Arg.Any<ApplicationReady>());
    }

    [Fact(DisplayName = "Resolves from the container with and without a message bus")]
    public async Task Resolves_With_And_Without_A_Bus()
    {
        var withoutBus = new ServiceCollection()
            .AddLogging()
            .AddAsyncInitialization()
            .BuildServiceProvider();
        var plainHost = withoutBus.GetRequiredService<IAsyncInitializationHost>();
        await plainHost.InitializeAsync(TestContext.Current.CancellationToken);
        plainHost.IsInitialized.ShouldBeTrue();

        var bus = Substitute.For<IMessageBus>();
        var withBus = new ServiceCollection()
            .AddLogging()
            .AddSingleton(bus)
            .AddAsyncInitialization()
            .BuildServiceProvider();
        await withBus.GetRequiredService<IAsyncInitializationHost>().InitializeAsync(TestContext.Current.CancellationToken);
        await bus.Received(1).PublishAsync(Arg.Any<ApplicationReady>());
    }

    private sealed class TestHook(string name, int order, Exception? failure = null, Task? waitFor = null) : IAsyncInitializationHook
    {
        public int Runs { get; private set; }

        public int Order => order;

        public string Name => name;

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            Runs++;
            if (waitFor is not null)
                await waitFor;
            if (failure is not null)
                throw failure;
        }
    }
}
