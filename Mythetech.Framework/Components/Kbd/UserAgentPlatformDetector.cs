namespace Mythetech.Framework.Components.Kbd;

/// <summary>
/// Platform detector for Blazor Server, which reads the platform from the browser's user agent.
/// A circuit can't read its own HTTP request, so the host reads the <c>User-Agent</c> header in
/// <c>App.razor</c>, passes it to its interactive root component as a parameter, and the root
/// calls <see cref="SetUserAgent"/> in <c>OnInitialized</c>. Register it with
/// <see cref="PlatformDetectorRegistrationExtensions.AddUserAgentPlatformDetector"/>.
/// </summary>
public sealed class UserAgentPlatformDetector : IPlatformDetector
{
    /// <inheritdoc />
    public bool IsMacOS { get; private set; }

    /// <summary>Sets the platform from the user agent of the request that started this circuit.</summary>
    /// <param name="userAgent">The request's <c>User-Agent</c> header, or null when it had none.</param>
    public void SetUserAgent(string? userAgent)
    {
        // Matches the WebAssembly detector; iPad and iPhone report "like Mac OS X" and use the same shortcuts.
        IsMacOS = userAgent is not null
            && (userAgent.Contains("Macintosh", StringComparison.Ordinal) || userAgent.Contains("Mac OS", StringComparison.Ordinal));
    }
}
