namespace Mythetech.Framework.Infrastructure;

/// <summary>
/// Abstract interface for opening links
/// </summary>
public interface ILinkOpenService
{
    /// <summary>
    /// Open a URL link
    /// </summary>
    /// <param name="url">A url to open</param>
    public Task OpenLinkAsync(string url);
}