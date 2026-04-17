using System.Runtime.InteropServices;
using Mythetech.Framework.Components.Kbd;

namespace Mythetech.Framework.Desktop.Components;

/// <summary>
/// Desktop platform detector using <see cref="RuntimeInformation"/>.
/// </summary>
public sealed class DesktopPlatformDetector : IPlatformDetector
{
    public bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
}
