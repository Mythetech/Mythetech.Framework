using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Components.Links;
using Mythetech.Framework.Infrastructure;
using NSubstitute;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Links;

public class ExternalLinkTests : TestContext
{
    private readonly ILinkOpenService _linkOpenService;

    public ExternalLinkTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;

        _linkOpenService = Substitute.For<ILinkOpenService>();
        Services.AddSingleton(_linkOpenService);
    }

    [Fact(DisplayName = "ExternalLink renders child content")]
    public void ExternalLink_RendersChildContent()
    {
        // Arrange & Act
        var cut = RenderComponent<ExternalLink>(parameters => parameters
            .Add(p => p.Link, "https://example.com")
            .AddChildContent("Click here"));

        // Assert
        cut.Markup.ShouldContain("Click here");
    }

    [Fact(DisplayName = "ExternalLink opens link via service when clicked")]
    public async Task ExternalLink_OpensLinkViaService_WhenClicked()
    {
        // Arrange
        var link = "https://example.com";
        var cut = RenderComponent<ExternalLink>(parameters => parameters
            .Add(p => p.Link, link)
            .AddChildContent("Visit"));

        // Act
        var mudLink = cut.Find(".mud-link");
        await mudLink.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await _linkOpenService.Received(1).OpenLinkAsync(link);
    }

    [Fact(DisplayName = "ExternalLink handles null link gracefully")]
    public async Task ExternalLink_HandlesNullLink_Gracefully()
    {
        // Arrange
        var cut = RenderComponent<ExternalLink>(parameters => parameters
            .AddChildContent("No link"));

        // Act
        var mudLink = cut.Find(".mud-link");
        await mudLink.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        await _linkOpenService.Received(1).OpenLinkAsync("");
    }

    [Fact(DisplayName = "ExternalLink applies default color")]
    public void ExternalLink_AppliesDefaultColor()
    {
        // Arrange & Act
        var cut = RenderComponent<ExternalLink>(parameters => parameters
            .Add(p => p.Link, "https://example.com")
            .AddChildContent("Link"));

        // Assert - Default color should not have a specific color class
        var mudLink = cut.Find(".mud-link");
        mudLink.ClassList.ShouldContain("mud-default-text");
    }

    [Fact(DisplayName = "ExternalLink applies custom color")]
    public void ExternalLink_AppliesCustomColor()
    {
        // Arrange & Act
        var cut = RenderComponent<ExternalLink>(parameters => parameters
            .Add(p => p.Link, "https://example.com")
            .Add(p => p.Color, Color.Primary)
            .AddChildContent("Link"));

        // Assert
        var mudLink = cut.Find(".mud-link");
        mudLink.ClassList.ShouldContain("mud-primary-text");
    }

    [Fact(DisplayName = "ExternalLink applies custom class")]
    public void ExternalLink_AppliesCustomClass()
    {
        // Arrange & Act
        var cut = RenderComponent<ExternalLink>(parameters => parameters
            .Add(p => p.Link, "https://example.com")
            .Add(p => p.Class, "my-custom-class")
            .AddChildContent("Link"));

        // Assert
        var mudLink = cut.Find(".mud-link");
        mudLink.ClassList.ShouldContain("my-custom-class");
    }

    [Fact(DisplayName = "ExternalLink applies custom style")]
    public void ExternalLink_AppliesCustomStyle()
    {
        // Arrange & Act
        var cut = RenderComponent<ExternalLink>(parameters => parameters
            .Add(p => p.Link, "https://example.com")
            .Add(p => p.Style, "font-weight: bold;")
            .AddChildContent("Link"));

        // Assert
        var mudLink = cut.Find(".mud-link");
        mudLink.GetAttribute("style")!.ShouldContain("font-weight: bold");
    }

    [Fact(DisplayName = "ExternalLink applies custom typography")]
    public void ExternalLink_AppliesCustomTypography()
    {
        // Arrange & Act
        var cut = RenderComponent<ExternalLink>(parameters => parameters
            .Add(p => p.Link, "https://example.com")
            .Add(p => p.Typo, Typo.h6)
            .AddChildContent("Heading Link"));

        // Assert
        var mudLink = cut.Find(".mud-link");
        mudLink.ClassList.ShouldContain("mud-typography-h6");
    }
}
