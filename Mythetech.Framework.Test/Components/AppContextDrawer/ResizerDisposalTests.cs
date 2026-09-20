using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using NSubstitute;

namespace Mythetech.Framework.Test.Components.AppContextDrawer;

public class ResizerDisposalTests : BunitContext
{
    private const string ModulePath = "./_content/Mythetech.Framework/mythetech.js";

    public ResizerDisposalTests()
    {
        Services.AddMudServices();
        Services.AddSingleton(Substitute.For<IMessageBus>());
        Services.AddSingleton(new PluginState());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "Disposing the drawer tears the resizer down before releasing the module")]
    public void Dispose_TearsDownResizer()
    {
        var module = JSInterop.SetupModule(ModulePath);

        Render<ResizerTestHost>();
        module.VerifyInvoke("initializeDrawerResizer");

        Renderer.DisposeComponents();

        module.VerifyInvoke("teardownDrawerResizer");
    }
}
