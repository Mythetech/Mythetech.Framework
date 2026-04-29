using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Mythetech.Framework.Components.Navigation;

/// <summary>
/// A shadcn-inspired navigation link that highlights based on the current route.
/// Supports both expanded (icon + text) and collapsed (icon-only with tooltip) modes.
/// </summary>
public partial class MtNavLink : MudComponentBase, IDisposable
{
    private string? _hrefAbsolute;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// The URL this link navigates to.
    /// </summary>
    [Parameter, EditorRequired]
    public string Href { get; set; } = "";

    /// <summary>
    /// The icon displayed in the link.
    /// </summary>
    [Parameter, EditorRequired]
    public string Icon { get; set; } = "";

    /// <summary>
    /// The text label displayed next to the icon.
    /// </summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = "";

    /// <summary>
    /// How the link URL is matched against the current location to determine active state.
    /// Defaults to <see cref="NavLinkMatch.Prefix"/>.
    /// </summary>
    [Parameter]
    public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;

    /// <summary>
    /// When true, renders a compact icon-only link with a tooltip showing the text.
    /// </summary>
    [Parameter]
    public bool IsCollapsed { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _hrefAbsolute = NavigationManager.ToAbsoluteUri(Href).AbsoluteUri;
    }

    private bool IsActive()
    {
        if (_hrefAbsolute == null) return false;

        var currentUri = NavigationManager.Uri;

        if (string.Equals(currentUri, _hrefAbsolute, StringComparison.OrdinalIgnoreCase))
            return true;

        if (currentUri.Length == _hrefAbsolute.Length - 1
            && _hrefAbsolute[^1] == '/'
            && _hrefAbsolute.StartsWith(currentUri, StringComparison.OrdinalIgnoreCase))
            return true;

        if (Match == NavLinkMatch.Prefix
            && currentUri.Length > _hrefAbsolute.Length
            && currentUri.StartsWith(_hrefAbsolute, StringComparison.OrdinalIgnoreCase)
            && (_hrefAbsolute[^1] == '/' || currentUri[_hrefAbsolute.Length] == '/'))
            return true;

        return false;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        InvokeAsync(StateHasChanged);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
