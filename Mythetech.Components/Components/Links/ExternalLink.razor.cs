namespace Mythetech.Components.Components.Links;

/// <summary>
/// A wrapper around a link component to support opening external links agnostic of hosting environment
/// For WebAssembly, it's desireable just to open the link in a new tab since you're already in the browser
/// For Desktop, you want it to be handled externally from the web view by the desktop OS
/// </summary>
public partial class ExternalLink
{
}