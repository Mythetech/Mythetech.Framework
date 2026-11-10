using Mythetech.Framework.Infrastructure.Plugins;
using Shouldly;

namespace Mythetech.Framework.Test.Infrastructure.Plugins;

public class PluginContextTests
{
    [Fact(DisplayName = "PluginContext does not expose the service provider")]
    public void PluginContext_DoesNotExposeServiceProvider()
    {
        typeof(PluginContext).GetProperty("Services").ShouldBeNull();
    }
}
