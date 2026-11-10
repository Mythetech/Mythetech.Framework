using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Components.CommandPalette;
using Mythetech.Framework.Components.Kbd;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Kbd;

public class UserAgentPlatformDetectorTests
{
    private const string MacUserAgent =
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36";

    [Theory(DisplayName = "Reads macOS from the browser's user agent")]
    [InlineData(MacUserAgent, true)]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Safari/605.1.15", true)]
    [InlineData("Mozilla/5.0 (iPad; CPU OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1", true)]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36", false)]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64; rv:132.0) Gecko/20100101 Firefox/132.0", false)]
    [InlineData("", false)]
    public void SetUserAgent_ReadsMacOS(string userAgent, bool expected)
    {
        var detector = new UserAgentPlatformDetector();

        detector.SetUserAgent(userAgent);

        detector.IsMacOS.ShouldBe(expected);
    }

    [Fact(DisplayName = "Reports non-macOS when the request carried no user agent")]
    public void SetUserAgent_Null_ReportsNonMacOS()
    {
        var detector = new UserAgentPlatformDetector();

        detector.SetUserAgent(null);

        detector.IsMacOS.ShouldBeFalse();
    }

    [Fact(DisplayName = "Each scope gets its own detector, so one user's platform never reaches another")]
    public void AddUserAgentPlatformDetector_IsolatesScopes()
    {
        var services = new ServiceCollection();
        services.AddUserAgentPlatformDetector();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var macCircuit = provider.CreateScope();
        using var otherCircuit = provider.CreateScope();

        macCircuit.ServiceProvider.GetRequiredService<UserAgentPlatformDetector>().SetUserAgent(MacUserAgent);

        macCircuit.ServiceProvider.GetRequiredService<IPlatformDetector>().IsMacOS.ShouldBeTrue();
        otherCircuit.ServiceProvider.GetRequiredService<IPlatformDetector>().IsMacOS.ShouldBeFalse();
    }

    [Theory(DisplayName = "Replaces the command palette's default detector whichever is registered first")]
    [InlineData(true)]
    [InlineData(false)]
    public void AddUserAgentPlatformDetector_ReplacesTheCommandPaletteDefault(bool paletteFirst)
    {
        var services = new ServiceCollection();
        if (paletteFirst)
        {
            services.AddCommandPalette();
            services.AddUserAgentPlatformDetector();
        }
        else
        {
            services.AddUserAgentPlatformDetector();
            services.AddCommandPalette();
        }

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<UserAgentPlatformDetector>().SetUserAgent(MacUserAgent);

        scope.ServiceProvider.GetRequiredService<IPlatformDetector>().IsMacOS.ShouldBeTrue();
    }
}
