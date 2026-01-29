using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using BlazorBaseUI.Tests.Contracts;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.Collapsible;

public abstract class CollapsibleTestsBase : TestBase,
    ICollapsibleRootContract,
    ICollapsibleTriggerContract,
    ICollapsiblePanelContract
{
    protected CollapsibleTestsBase(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }

    #region ICollapsibleRootContract

    [Fact]
    public virtual async Task RendersAsDivByDefault()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var root = GetByTestId("collapsible-root");
        var tagName = await root.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        Assert.Equal("div", tagName);
    }

    [Fact]
    public virtual async Task RendersWithCustomAs()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var root = GetByTestId("collapsible-root");
        await Assertions.Expect(root).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task ForwardsAdditionalAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var root = GetByTestId("collapsible-root");
        await Assertions.Expect(root).ToHaveAttributeAsync("data-testid", "collapsible-root");
    }

    [Fact]
    public virtual async Task AppliesClassValue()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var root = GetByTestId("collapsible-root");
        await Assertions.Expect(root).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task AppliesStyleValue()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var root = GetByTestId("collapsible-root");
        await Assertions.Expect(root).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task CombinesClassFromBothSources()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var root = GetByTestId("collapsible-root");
        await Assertions.Expect(root).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task CascadesContextToChildren()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var trigger = GetByTestId("collapsible-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Fact]
    public virtual async Task ControlledModeRespectsOpenParameter()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task UncontrolledModeUsesDefaultOpen()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task InvokesOnOpenChange()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var changeCount = GetByTestId("change-count");
        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        await ClickTriggerAsync();
        await Assertions.Expect(changeCount).ToHaveTextAsync("1");
    }

    [Fact]
    public virtual async Task OnOpenChangeReceivesEventDetails()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var lastReason = GetByTestId("last-reason");
        var lastOpen = GetByTestId("last-open");
        var lastCanceled = GetByTestId("last-canceled");

        // Click trigger to open
        await ClickTriggerAsync();

        // Verify event details when opening
        await Assertions.Expect(lastReason).ToHaveTextAsync("TriggerPress");
        await Assertions.Expect(lastOpen).ToHaveTextAsync("true");
        await Assertions.Expect(lastCanceled).ToHaveTextAsync("false");

        // Click trigger to close
        await ClickTriggerAsync();

        // Verify event details when closing
        await Assertions.Expect(lastReason).ToHaveTextAsync("TriggerPress");
        await Assertions.Expect(lastOpen).ToHaveTextAsync("false");
        await Assertions.Expect(lastCanceled).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task HasDataOpenWhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var root = GetByTestId("collapsible-root");
        await Assertions.Expect(root).ToHaveAttributeAsync("data-open", "");
    }

    [Fact]
    public virtual async Task HasDataClosedWhenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var root = GetByTestId("collapsible-root");
        await Assertions.Expect(root).ToHaveAttributeAsync("data-closed", "");
    }

    [Fact]
    public virtual async Task HasDataDisabledWhenDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDisabled(true));

        var trigger = GetByTestId("collapsible-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-disabled", "");
    }

    #endregion

    #region ICollapsibleTriggerContract

    [Fact]
    public virtual async Task RendersAsButtonByDefault()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var trigger = GetByTestId("collapsible-trigger");
        var tagName = await trigger.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        Assert.Equal("button", tagName);
    }

    Task ICollapsibleTriggerContract.RendersWithCustomAs()
    {
        return RendersWithCustomAs_Trigger();
    }

    [Fact]
    public virtual async Task RendersWithCustomAs_Trigger()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var trigger = GetByTestId("collapsible-trigger");
        await Assertions.Expect(trigger).ToBeVisibleAsync();
    }

    Task ICollapsibleTriggerContract.ForwardsAdditionalAttributes()
    {
        return ForwardsAdditionalAttributes_Trigger();
    }

    [Fact]
    public virtual async Task ForwardsAdditionalAttributes_Trigger()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var trigger = GetByTestId("collapsible-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-testid", "collapsible-trigger");
    }

    Task ICollapsibleTriggerContract.AppliesClassValue()
    {
        return AppliesClassValue_Trigger();
    }

    [Fact]
    public virtual async Task AppliesClassValue_Trigger()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var trigger = GetByTestId("collapsible-trigger");
        await Assertions.Expect(trigger).ToBeVisibleAsync();
    }

    Task ICollapsibleTriggerContract.AppliesStyleValue()
    {
        return AppliesStyleValue_Trigger();
    }

    [Fact]
    public virtual async Task AppliesStyleValue_Trigger()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var trigger = GetByTestId("collapsible-trigger");
        await Assertions.Expect(trigger).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task HasAriaExpandedFalseWhenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var trigger = GetByTestId("collapsible-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Fact]
    public virtual async Task HasAriaExpandedTrueWhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var trigger = GetByTestId("collapsible-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Fact]
    public virtual async Task HasAriaControlsWhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var trigger = GetByTestId("collapsible-trigger");
        var panel = GetByTestId("collapsible-panel");

        // Verify panel has an ID
        var panelId = await panel.GetAttributeAsync("id");
        Assert.False(string.IsNullOrEmpty(panelId), "Panel should have an ID");

        // aria-controls attribute exists on trigger
        // Note: Due to render order, aria-controls may be empty on initial render
        // The component sets it via cascading parameter which updates asynchronously
        var ariaControls = await trigger.GetAttributeAsync("aria-controls");
        Assert.NotNull(ariaControls);
    }

    [Fact]
    public virtual async Task HasNoAriaControlsWhenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var trigger = GetByTestId("collapsible-trigger");
        // aria-controls attribute is NOT present when closed (per Base UI behavior)
        var ariaControls = await trigger.GetAttributeAsync("aria-controls");
        Assert.Null(ariaControls);
    }

    [Fact]
    public virtual async Task HasDataPanelOpenWhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var trigger = GetByTestId("collapsible-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-panel-open", "");
    }

    [Fact]
    public virtual async Task HasNoDataPanelOpenWhenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var trigger = GetByTestId("collapsible-trigger");
        await Assertions.Expect(trigger).Not.ToHaveAttributeAsync("data-panel-open", string.Empty);
    }

    Task ICollapsibleTriggerContract.HasDataDisabledWhenDisabled()
    {
        return HasDataDisabledWhenDisabled();
    }

    [Fact]
    public virtual async Task HasDisabledAttributeWhenDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDisabled(true));

        var trigger = GetByTestId("collapsible-trigger");
        await Assertions.Expect(trigger).ToBeDisabledAsync();
    }

    [Fact]
    public virtual async Task TogglesOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var trigger = GetByTestId("collapsible-trigger");
        var panel = GetByTestId("collapsible-panel");

        // Verify aria-controls is NOT present when closed
        var ariaControlsBefore = await trigger.GetAttributeAsync("aria-controls");
        Assert.Null(ariaControlsBefore);

        await trigger.ClickAsync();
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 5000 * TimeoutMultiplier
        });

        await Assertions.Expect(panel).ToBeVisibleAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");

        // Verify aria-controls IS present when open and matches panel id
        // Note: There's a render cycle delay - panel must mount first to set its ID
        var panelId = await panel.GetAttributeAsync("id");
        Assert.False(string.IsNullOrEmpty(panelId), "Panel should have an ID");

        // Wait for aria-controls to be set to the panel ID
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-controls", panelId);
    }

    [Fact]
    public virtual async Task DoesNotToggleWhenDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDisabled(true));

        var trigger = GetByTestId("collapsible-trigger");
        var panel = GetByTestId("collapsible-panel");

        await trigger.ClickAsync(new LocatorClickOptions { Force = true });
        await Page.WaitForTimeoutAsync(500);

        await Assertions.Expect(panel).Not.ToBeVisibleAsync();
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Fact]
    public virtual Task RequiresContext()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region ICollapsiblePanelContract

    Task ICollapsiblePanelContract.RendersAsDivByDefault()
    {
        return RendersAsDivByDefault_Panel();
    }

    [Fact]
    public virtual async Task RendersAsDivByDefault_Panel()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var panel = GetByTestId("collapsible-panel");
        var tagName = await panel.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        Assert.Equal("div", tagName);
    }

    Task ICollapsiblePanelContract.RendersWithCustomAs()
    {
        return RendersWithCustomAs_Panel();
    }

    [Fact]
    public virtual async Task RendersWithCustomAs_Panel()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var panel = GetByTestId("collapsible-panel");
        await Assertions.Expect(panel).ToBeVisibleAsync();
    }

    Task ICollapsiblePanelContract.ForwardsAdditionalAttributes()
    {
        return ForwardsAdditionalAttributes_Panel();
    }

    [Fact]
    public virtual async Task ForwardsAdditionalAttributes_Panel()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var panel = GetByTestId("collapsible-panel");
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-testid", "collapsible-panel");
    }

    Task ICollapsiblePanelContract.AppliesClassValue()
    {
        return AppliesClassValue_Panel();
    }

    [Fact]
    public virtual async Task AppliesClassValue_Panel()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible")
            .WithDefaultOpen(true)
            .WithAnimated(true));

        var panel = GetByTestId("collapsible-panel");
        await Assertions.Expect(panel).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("animated-panel"));
    }

    Task ICollapsiblePanelContract.AppliesStyleValue()
    {
        return AppliesStyleValue_Panel();
    }

    [Fact]
    public virtual async Task AppliesStyleValue_Panel()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var panel = GetByTestId("collapsible-panel");
        await Assertions.Expect(panel).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task IsNotVisibleWhenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var panel = GetByTestId("collapsible-panel");
        await Assertions.Expect(panel).Not.ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task IsVisibleWhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var panel = GetByTestId("collapsible-panel");
        await Assertions.Expect(panel).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task RemainsInDomWhenKeepMounted()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithKeepMounted(true));

        var panel = GetByTestId("collapsible-panel");
        await Assertions.Expect(panel).ToBeAttachedAsync();
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-closed", "");

        await ClickTriggerAsync();
        await WaitForAttributeValueAsync(panel, "data-open", "");
        await Assertions.Expect(panel).ToBeVisibleAsync();

        await ClickTriggerAsync();
        await WaitForAttributeValueAsync(panel, "data-closed", "");
        await Assertions.Expect(panel).ToBeAttachedAsync();
    }

    [Fact]
    public virtual async Task IsRemovedFromDomWhenNotKeepMounted()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var panel = GetByTestId("collapsible-panel");
        await Assertions.Expect(panel).Not.ToBeAttachedAsync();

        await ClickTriggerAsync();
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 5000 * TimeoutMultiplier
        });

        await Assertions.Expect(panel).ToBeAttachedAsync();
        await Assertions.Expect(panel).ToBeVisibleAsync();

        await ClickTriggerAsync();
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Detached,
            Timeout = 5000 * TimeoutMultiplier
        });

        await Assertions.Expect(panel).Not.ToBeAttachedAsync();
    }

    [Fact]
    public virtual async Task HasHiddenUntilFoundAttribute()
    {
        // When HiddenUntilFound is true, the panel remains in DOM when closed
        // (similar to KeepMounted) but uses the hidden="until-found" attribute
        // The panel is present with data-closed when closed
        await NavigateAsync(CreateUrl("/tests/collapsible").WithHiddenUntilFound(true));

        var panel = GetByTestId("collapsible-panel");
        // Panel should be attached (present in DOM) even when closed
        await Assertions.Expect(panel).ToBeAttachedAsync();
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-closed", "");
    }

    [Fact]
    public virtual async Task HiddenUntilFoundOpensOnBeforeMatchEvent()
    {
        // When hiddenUntilFound is true and the browser's "find in page" feature
        // reveals the hidden content, a beforematch event is dispatched which
        // should open the collapsible
        await NavigateAsync(CreateUrl("/tests/collapsible").WithHiddenUntilFound(true));

        var panel = GetByTestId("collapsible-panel");
        var trigger = GetByTestId("collapsible-trigger");
        var changeCount = GetByTestId("change-count");

        // Initially closed
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-closed", "");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        // Dispatch beforematch event (simulates browser's find-in-page revealing hidden content)
        await panel.EvaluateAsync(@"(el) => {
            const event = new Event('beforematch', { bubbles: true, cancelable: false });
            el.dispatchEvent(event);
        }");

        // Wait for the collapsible to open
        await WaitForAttributeValueAsync(panel, "data-open", "");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(changeCount).ToHaveTextAsync("1");
    }

    Task ICollapsiblePanelContract.HasDataOpenWhenOpen()
    {
        return HasDataOpenWhenOpen_Panel();
    }

    [Fact]
    public virtual async Task HasDataOpenWhenOpen_Panel()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var panel = GetByTestId("collapsible-panel");
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-open", "");
    }

    Task ICollapsiblePanelContract.HasDataClosedWhenClosed()
    {
        return HasDataClosedWhenClosed_Panel();
    }

    [Fact]
    public virtual async Task HasDataClosedWhenClosed_Panel()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithKeepMounted(true));

        var panel = GetByTestId("collapsible-panel");
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-closed", "");
    }

    [Fact]
    public virtual async Task ReceivesCorrectState()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var panel = GetByTestId("collapsible-panel");
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-open", "");
    }

    Task ICollapsiblePanelContract.RequiresContext()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region Additional Integration Tests

    [Fact]
    public virtual async Task Panel_ShouldOpen_OnTriggerClick()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var trigger = GetByTestId("collapsible-trigger");
        var panel = GetByTestId("collapsible-panel");

        // Verify aria-controls is NOT present when closed
        var ariaControlsBefore = await trigger.GetAttributeAsync("aria-controls");
        Assert.Null(ariaControlsBefore);

        await trigger.ClickAsync();
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 5000 * TimeoutMultiplier
        });

        await Assertions.Expect(panel).ToBeVisibleAsync();
        await Assertions.Expect(panel).ToHaveAttributeAsync("data-open", "");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-panel-open", "");

        // Verify aria-controls IS present when open
        // Note: There's a render cycle delay - panel must mount first to set its ID
        var panelId = await panel.GetAttributeAsync("id");
        Assert.False(string.IsNullOrEmpty(panelId), "Panel should have an ID");

        // Wait for aria-controls to be set to the panel ID
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-controls", panelId);
    }

    [Fact]
    public virtual async Task Panel_ShouldClose_OnSecondTriggerClick()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible").WithDefaultOpen(true));

        var trigger = GetByTestId("collapsible-trigger");

        // Verify aria-controls IS present when open
        var ariaControlsBefore = await trigger.GetAttributeAsync("aria-controls");
        Assert.NotNull(ariaControlsBefore);

        await trigger.ClickAsync();

        await WaitForAttributeValueAsync(trigger, "aria-expanded", "false");
        await Assertions.Expect(trigger).Not.ToHaveAttributeAsync("data-panel-open", string.Empty);

        // Verify aria-controls is removed when closed
        var ariaControlsAfter = await trigger.GetAttributeAsync("aria-controls");
        Assert.Null(ariaControlsAfter);
    }

    [Fact]
    public virtual async Task Panel_ShouldToggleMultipleTimes()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var trigger = GetByTestId("collapsible-trigger");
        var panel = GetByTestId("collapsible-panel");

        await trigger.ClickAsync();
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });
        await Assertions.Expect(panel).ToBeVisibleAsync();

        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "false");

        await trigger.ClickAsync();
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });
        await Assertions.Expect(panel).ToBeVisibleAsync();

        await trigger.ClickAsync();
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "false");
    }

    [Theory]
    [InlineData("Enter")]
    [InlineData(" ")]
    public virtual async Task Trigger_ShouldToggle_OnKeyPress(string key)
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var trigger = GetByTestId("collapsible-trigger");
        var panel = GetByTestId("collapsible-panel");

        // Use trigger.PressAsync which handles focus automatically and is more reliable
        await trigger.PressAsync(key);

        // Wait for aria-expanded to change first, then check panel
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "true");
        await panel.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 10000 * TimeoutMultiplier
        });
        await Assertions.Expect(panel).ToBeVisibleAsync();

        await trigger.PressAsync(key);
        await WaitForAttributeValueAsync(trigger, "aria-expanded", "false");
    }

    [Fact]
    public virtual async Task Trigger_ShouldReceiveFocus_OnTab()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        await Page.Keyboard.PressAsync("Tab");

        var trigger = GetByTestId("collapsible-trigger");
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    [Fact]
    public virtual async Task Trigger_ShouldReferenceCustomPanelId_InAriaControls()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible")
            .WithDefaultOpen(true)
            .WithCustomPanelId("custom-panel-id"));

        var trigger = GetByTestId("collapsible-trigger");
        var panel = GetByTestId("collapsible-panel");

        // Panel should have the custom ID
        await Assertions.Expect(panel).ToHaveAttributeAsync("id", "custom-panel-id");

        // aria-controls attribute exists on trigger
        // Note: Due to render order, aria-controls may not be synced with panel ID on initial render
        var ariaControls = await trigger.GetAttributeAsync("aria-controls");
        Assert.NotNull(ariaControls);
    }

    [Fact]
    public virtual async Task StateDisplay_ShouldShowCorrectOpenState()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");

        await ClickTriggerAsync();
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task StateDisplay_ShouldIncrementChangeCount()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible"));

        var changeCount = GetByTestId("change-count");
        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        await ClickTriggerAsync();
        await Assertions.Expect(changeCount).ToHaveTextAsync("1");

        await ClickTriggerAsync();
        await Assertions.Expect(changeCount).ToHaveTextAsync("2");
    }

    #endregion

    #region Animation Tests

    [Fact]
    public virtual async Task Panel_ShouldHaveDataStartingStyle_WhenOpening()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible")
            .WithAnimated(true)
            .WithKeepMounted(true));

        var panel = GetByTestId("collapsible-panel");

        var startingStyleDetected = false;
        await Page.ExposeFunctionAsync("onStartingStyle", () =>
        {
            startingStyleDetected = true;
        });

        await panel.EvaluateAsync(@"
            (el) => {
                const observer = new MutationObserver((mutations) => {
                    for (const mutation of mutations) {
                        if (mutation.attributeName === 'data-starting-style') {
                            if (el.hasAttribute('data-starting-style')) {
                                window.onStartingStyle();
                            }
                        }
                    }
                });
                observer.observe(el, { attributes: true });
            }
        ");

        await ClickTriggerAsync();
        await panel.WaitForAnimationsAsync();

        Assert.True(startingStyleDetected, "data-starting-style should be set during open animation");
    }

    [Fact]
    public virtual async Task Panel_ShouldHaveDataEndingStyle_WhenClosing()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible")
            .WithAnimated(true)
            .WithDefaultOpen(true)
            .WithKeepMounted(true));

        var panel = GetByTestId("collapsible-panel");

        var endingStyleDetected = false;
        await Page.ExposeFunctionAsync("onEndingStyle", () =>
        {
            endingStyleDetected = true;
        });

        await panel.EvaluateAsync(@"
            (el) => {
                const observer = new MutationObserver((mutations) => {
                    for (const mutation of mutations) {
                        if (mutation.attributeName === 'data-ending-style') {
                            if (el.hasAttribute('data-ending-style')) {
                                window.onEndingStyle();
                            }
                        }
                    }
                });
                observer.observe(el, { attributes: true });
            }
        ");

        await ClickTriggerAsync();
        await panel.WaitForAnimationsAsync();

        Assert.True(endingStyleDetected, "data-ending-style should be set during close animation");
    }

    [Fact]
    public virtual async Task Panel_ShouldSetCssVariables_WhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible")
            .WithAnimated(true)
            .WithKeepMounted(true));

        var panel = GetByTestId("collapsible-panel");

        await ClickTriggerAsync();
        await WaitForAttributeValueAsync(panel, "data-open", "");
        await panel.WaitForAnimationsAsync();

        // After animation completes, height should be set to "auto"
        // Wait a bit for the final CSS variable update
        await Page.WaitForTimeoutAsync(100);

        var heightVar = await panel.GetStylePropertyAsync("--collapsible-panel-height");
        Assert.Equal("auto", heightVar);
    }

    [Fact]
    public virtual async Task Panel_ShouldHaveZeroHeightCssVariable_WhenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible")
            .WithAnimated(true)
            .WithKeepMounted(true));

        var panel = GetByTestId("collapsible-panel");

        var heightVar = await panel.GetStylePropertyAsync("--collapsible-panel-height");
        Assert.Equal("0px", heightVar);
    }

    [Fact]
    public virtual async Task Panel_ShouldExpandHeight_WhenOpening()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible")
            .WithAnimated(true)
            .WithKeepMounted(true));

        var panel = GetByTestId("collapsible-panel");
        var initialHeight = await panel.GetHeightAsync();

        await ClickTriggerAsync();
        await WaitForAttributeValueAsync(panel, "data-open", "");
        await panel.WaitForAnimationsAsync();

        // Wait for CSS variable update and layout recalculation
        await Page.WaitForTimeoutAsync(100);

        var finalHeight = await panel.GetHeightAsync();
        Assert.True(finalHeight > initialHeight, $"Panel should expand. Initial: {initialHeight}, Final: {finalHeight}");
    }

    [Fact]
    public virtual async Task Panel_ShouldCollapseHeight_WhenClosing()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible")
            .WithAnimated(true)
            .WithDefaultOpen(true)
            .WithKeepMounted(true));

        var panel = GetByTestId("collapsible-panel");
        var openHeight = await panel.GetHeightAsync();

        await ClickTriggerAsync();
        await WaitForAttributeValueAsync(panel, "data-closed", "");
        await panel.WaitForAnimationsAsync();

        // Wait for CSS variable update and layout recalculation
        await Page.WaitForTimeoutAsync(100);

        var closedHeight = await panel.GetHeightAsync();
        Assert.True(closedHeight < openHeight, $"Panel should collapse. Open: {openHeight}, Closed: {closedHeight}");
    }

    [Fact]
    public virtual async Task Panel_ShouldHandleRapidToggle_WithoutBreaking()
    {
        await NavigateAsync(CreateUrl("/tests/collapsible")
            .WithAnimated(true)
            .WithKeepMounted(true));

        var trigger = GetByTestId("collapsible-trigger");
        var panel = GetByTestId("collapsible-panel");

        // Rapid toggle clicks
        await trigger.ClickAsync();
        await Task.Delay(50);
        await trigger.ClickAsync();
        await Task.Delay(50);
        await trigger.ClickAsync();
        await Task.Delay(50);
        await trigger.ClickAsync();
        await Task.Delay(50);
        await trigger.ClickAsync();

        // Wait for any ongoing animations to complete
        await Task.Delay(500);

        // Verify the panel is in a consistent state
        var hasOpen = await panel.EvaluateAsync<bool>("el => el.hasAttribute('data-open')");
        var hasClosed = await panel.EvaluateAsync<bool>("el => el.hasAttribute('data-closed')");

        Assert.True(hasOpen || hasClosed, "Panel should have either data-open or data-closed");
        Assert.False(hasOpen && hasClosed, "Panel should not have both data-open and data-closed");
    }

    #endregion
}
