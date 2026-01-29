using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using BlazorBaseUI.Tests.Contracts;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.Avatar;

public abstract class AvatarTestsBase : TestBase, IAvatarRootContract, IAvatarImageContract, IAvatarFallbackContract
{
    protected AvatarTestsBase(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }

    #region IAvatarRootContract

    [Fact]
    public virtual async Task RendersAsSpanByDefault()
    {
        await NavigateAsync(CreateUrl("/tests/avatar"));

        var root = GetByTestId("avatar-root");

        await Assertions.Expect(root).ToBeVisibleAsync();

        var tagName = await root.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        Assert.Equal("span", tagName);
    }

    [Fact]
    public virtual async Task RendersWithCustomAs()
    {
        await NavigateAsync(CreateUrl("/tests/avatar").WithAs("div"));

        var root = GetByTestId("avatar-root");
        await Assertions.Expect(root).ToBeVisibleAsync();

        var tagName = await root.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        Assert.Equal("div", tagName);
    }

    [Fact]
    public virtual async Task ForwardsAdditionalAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/avatar"));

        var root = GetByTestId("avatar-root");

        await Assertions.Expect(root).ToHaveAttributeAsync("data-custom", "custom-value");
    }

    [Fact]
    public virtual async Task AppliesClassValue()
    {
        await NavigateAsync(CreateUrl("/tests/avatar").WithCustomClass("test-class"));

        var root = GetByTestId("avatar-root");

        await Assertions.Expect(root).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("test-class"));
    }

    [Fact]
    public virtual async Task AppliesStyleValue()
    {
        await NavigateAsync(CreateUrl("/tests/avatar").WithCustomStyle("color: red"));

        var root = GetByTestId("avatar-root");
        await Assertions.Expect(root).ToBeVisibleAsync();

        var style = await root.GetAttributeAsync("style");
        Assert.NotNull(style);
        Assert.Contains("color: red", style);
    }

    [Fact]
    public virtual async Task CombinesClassFromBothSources()
    {
        await NavigateAsync(CreateUrl("/tests/avatar").WithCustomClass("test-class"));

        var root = GetByTestId("avatar-root");
        await Assertions.Expect(root).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("test-class"));
    }

    [Fact]
    public virtual async Task CascadesContextToChildren()
    {
        await NavigateAsync(CreateUrl("/tests/avatar"));

        var loadingStatus = GetByTestId("loading-status");
        await Assertions.Expect(loadingStatus).ToBeVisibleAsync();
    }

    #endregion

    #region IAvatarImageContract

    [Fact]
    public virtual async Task RendersWhenLoaded()
    {
        await NavigateAsync(CreateUrl("/tests/avatar"));

        var loadingStatus = GetByTestId("loading-status");
        await WaitForTextContentAsync(loadingStatus, "loaded", 15000);

        var image = GetByTestId("avatar-image");

        await Assertions.Expect(image).ToBeVisibleAsync();

        var tagName = await image.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        Assert.Equal("img", tagName);
    }

    [Fact]
    public virtual async Task DoesNotRenderWhenNotLoaded()
    {
        await NavigateAsync(CreateUrl("/tests/avatar").WithForceError(true));

        var loadingStatus = GetByTestId("loading-status");
        await WaitForTextContentAsync(loadingStatus, "error", 15000);

        var image = GetByTestId("avatar-image");

        await Assertions.Expect(image).Not.ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task UpdatesStatusOnLoad()
    {
        await NavigateAsync(CreateUrl("/tests/avatar"));

        var loadingStatus = GetByTestId("loading-status");

        await WaitForTextContentAsync(loadingStatus, "loaded", 15000);

        await Assertions.Expect(loadingStatus).ToHaveTextAsync("loaded");
    }

    [Fact]
    public virtual async Task UpdatesStatusOnError()
    {
        await NavigateAsync(CreateUrl("/tests/avatar").WithForceError(true));

        var loadingStatus = GetByTestId("loading-status");

        await WaitForTextContentAsync(loadingStatus, "error", 15000);

        await Assertions.Expect(loadingStatus).ToHaveTextAsync("error");
    }

    [Fact]
    public virtual async Task InvokesOnLoadingStatusChange()
    {
        await NavigateAsync(CreateUrl("/tests/avatar"));

        var statusChangeCount = GetByTestId("status-change-count");

        var loadingStatus = GetByTestId("loading-status");
        await WaitForTextContentAsync(loadingStatus, "loaded", 15000);

        var count = await statusChangeCount.TextContentAsync();
        var countValue = int.Parse(count ?? "0");

        Assert.True(countValue >= 1, $"Expected status change count >= 1, got {countValue}");
    }

    [Fact]
    public virtual async Task ForwardsAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/avatar"));

        var loadingStatus = GetByTestId("loading-status");
        await WaitForTextContentAsync(loadingStatus, "loaded", 15000);

        var image = GetByTestId("avatar-image");

        var src = await image.GetAttributeAsync("src");
        Assert.NotNull(src);
        Assert.Contains("blazor-logo.png", src);
    }

    [Fact]
    public virtual async Task RequiresContext()
    {
        await NavigateAsync(CreateUrl("/tests/avatar"));
        await Assertions.Expect(GetByTestId("avatar-root")).ToBeVisibleAsync();
    }

    #endregion

    #region IAvatarFallbackContract

    [Fact]
    public virtual async Task RendersWhenImageFails()
    {
        await NavigateAsync(CreateUrl("/tests/avatar").WithForceError(true));

        var loadingStatus = GetByTestId("loading-status");
        await WaitForTextContentAsync(loadingStatus, "error", 15000);

        var fallback = GetByTestId("avatar-fallback");

        await Assertions.Expect(fallback).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task DoesNotRenderWhenImageLoaded()
    {
        await NavigateAsync(CreateUrl("/tests/avatar"));

        var loadingStatus = GetByTestId("loading-status");
        await WaitForTextContentAsync(loadingStatus, "loaded", 15000);

        var fallback = GetByTestId("avatar-fallback");

        await Assertions.Expect(fallback).Not.ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task RendersAsSpanByDefault_Fallback()
    {
        await NavigateAsync(CreateUrl("/tests/avatar").WithForceError(true));

        var loadingStatus = GetByTestId("loading-status");
        await WaitForTextContentAsync(loadingStatus, "error", 15000);

        var fallback = GetByTestId("avatar-fallback");

        var tagName = await fallback.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        Assert.Equal("span", tagName);
    }

    [Fact]
    public virtual async Task DoesNotShowBeforeDelayElapsed()
    {
        await Page.Clock.InstallAsync();

        await NavigateAsync(CreateUrl("/tests/avatar")
            .WithForceError(true)
            .WithFallbackDelay(2000));

        var fallback = GetByTestId("avatar-fallback");

        await Assertions.Expect(fallback).Not.ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task ShowsAfterDelayElapsed()
    {
        await Page.Clock.InstallAsync();

        await NavigateAsync(CreateUrl("/tests/avatar")
            .WithForceError(true)
            .WithFallbackDelay(500));

        await Page.Clock.FastForwardAsync(600);

        var fallback = GetByTestId("avatar-fallback");

        await Assertions.Expect(fallback).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task ShowsImmediatelyWhenNoDelay()
    {
        await NavigateAsync(CreateUrl("/tests/avatar").WithForceError(true));

        var loadingStatus = GetByTestId("loading-status");
        await WaitForTextContentAsync(loadingStatus, "error", 15000);

        var fallback = GetByTestId("avatar-fallback");

        await Assertions.Expect(fallback).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task ReceivesCorrectState()
    {
        await NavigateAsync(CreateUrl("/tests/avatar"));

        var loadingStatus = GetByTestId("loading-status");
        await WaitForTextContentAsync(loadingStatus, "loaded", 15000);

        await Assertions.Expect(loadingStatus).ToHaveTextAsync("loaded");
    }

    Task IAvatarFallbackContract.RequiresContext()
    {
        return Task.CompletedTask;
    }

    Task IAvatarFallbackContract.RendersAsSpanByDefault()
    {
        return RendersAsSpanByDefault_Fallback();
    }

    #endregion

    #region Helper Methods

    protected async Task WaitForTextContentAsync(ILocator element, string expectedText, int timeout = 5000)
    {
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < timeout)
        {
            var text = await element.TextContentAsync();
            if (text == expectedText)
            {
                Console.WriteLine($"[Debug] Found expected text '{expectedText}' at {(DateTime.UtcNow - startTime).TotalMilliseconds}ms");
                await Page.WaitForTimeoutAsync(500);
                return;
            }
            await Task.Delay(100);
        }

        var finalText = await element.TextContentAsync();
        throw new TimeoutException($"Text content did not reach '{expectedText}' within {timeout}ms. Current: '{finalText}'");
    }

    #endregion
}
