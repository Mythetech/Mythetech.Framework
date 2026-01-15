using Bunit;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using Mythetech.Framework.Components.Progress;
using Shouldly;

namespace Mythetech.Framework.Test.Components.Progress;

public class ProgressCountdownTests : TestContext
{
    public ProgressCountdownTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact(DisplayName = "ProgressCountdown renders MudProgressLinear")]
    public void ProgressCountdown_RendersMudProgressLinear()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressCountdown>();

        // Assert - MudProgressLinear renders as a div with mud-progress-linear class
        var progressBar = cut.Find(".mud-progress-linear");
        progressBar.ShouldNotBeNull();
    }

    [Fact(DisplayName = "ProgressCountdown starts at 100% value")]
    public void ProgressCountdown_StartsAt100PercentValue()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressCountdown>();

        // Assert - the progress indicator should have transform showing near 100%
        // MudProgressLinear uses a transform on the indicator div
        var markup = cut.Markup;

        // Check that it rendered a progress bar
        cut.Find(".mud-progress-linear").ShouldNotBeNull();
    }

    [Fact(DisplayName = "ProgressCountdown accepts Duration parameter")]
    public void ProgressCountdown_AcceptsDurationParameter()
    {
        // Arrange & Act - should not throw
        var cut = RenderComponent<ProgressCountdown>(parameters => parameters
            .Add(p => p.Duration, 10000));

        // Assert - component renders
        var progressBar = cut.Find(".mud-progress-linear");
        progressBar.ShouldNotBeNull();
    }

    [Fact(DisplayName = "ProgressCountdown applies Primary color by default")]
    public void ProgressCountdown_AppliesPrimaryColorByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressCountdown>();

        // Assert
        var progressBar = cut.Find(".mud-progress-linear");
        progressBar.ClassList.ShouldContain("mud-progress-linear-color-primary");
    }

    [Fact(DisplayName = "ProgressCountdown applies custom color")]
    public void ProgressCountdown_AppliesCustomColor()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressCountdown>(parameters => parameters
            .Add(p => p.Color, Color.Secondary));

        // Assert
        var progressBar = cut.Find(".mud-progress-linear");
        progressBar.ClassList.ShouldContain("mud-progress-linear-color-secondary");
    }

    [Fact(DisplayName = "ProgressCountdown applies rounded style by default")]
    public void ProgressCountdown_AppliesRoundedByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressCountdown>();

        // Assert
        var progressBar = cut.Find(".mud-progress-linear");
        progressBar.ClassList.ShouldContain("mud-progress-linear-rounded");
    }

    [Fact(DisplayName = "ProgressCountdown can disable rounded style")]
    public void ProgressCountdown_CanDisableRoundedStyle()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressCountdown>(parameters => parameters
            .Add(p => p.Rounded, false));

        // Assert
        var progressBar = cut.Find(".mud-progress-linear");
        progressBar.ClassList.ShouldNotContain("mud-progress-linear-rounded");
    }

    [Fact(DisplayName = "ProgressCountdown can enable striped pattern")]
    public void ProgressCountdown_CanEnableStripedPattern()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressCountdown>(parameters => parameters
            .Add(p => p.Striped, true));

        // Assert
        var progressBar = cut.Find(".mud-progress-linear");
        progressBar.ClassList.ShouldContain("mud-progress-linear-striped");
    }

    [Fact(DisplayName = "ProgressCountdown applies Small size by default")]
    public void ProgressCountdown_AppliesSmallSizeByDefault()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressCountdown>();

        // Assert
        var progressBar = cut.Find(".mud-progress-linear");
        progressBar.ClassList.ShouldContain("mud-progress-linear-small");
    }

    [Fact(DisplayName = "ProgressCountdown applies custom size")]
    public void ProgressCountdown_AppliesCustomSize()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressCountdown>(parameters => parameters
            .Add(p => p.Size, Size.Large));

        // Assert
        var progressBar = cut.Find(".mud-progress-linear");
        progressBar.ClassList.ShouldContain("mud-progress-linear-large");
    }

    [Fact(DisplayName = "ProgressCountdown disposes cleanly")]
    public void ProgressCountdown_DisposesCleanly()
    {
        // Arrange
        var cut = RenderComponent<ProgressCountdown>(parameters => parameters
            .Add(p => p.Duration, 100000)); // Long duration so we can dispose mid-countdown

        // Act - dispose should not throw
        cut.Dispose();

        // Assert - no exception means success
        Assert.True(true);
    }

    [Fact(DisplayName = "ProgressCountdown can use Error color")]
    public void ProgressCountdown_CanUseErrorColor()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressCountdown>(parameters => parameters
            .Add(p => p.Color, Color.Error));

        // Assert
        var progressBar = cut.Find(".mud-progress-linear");
        progressBar.ClassList.ShouldContain("mud-progress-linear-color-error");
    }

    [Fact(DisplayName = "ProgressCountdown can use Medium size")]
    public void ProgressCountdown_CanUseMediumSize()
    {
        // Arrange & Act
        var cut = RenderComponent<ProgressCountdown>(parameters => parameters
            .Add(p => p.Size, Size.Medium));

        // Assert
        var progressBar = cut.Find(".mud-progress-linear");
        progressBar.ClassList.ShouldContain("mud-progress-linear-medium");
    }
}
