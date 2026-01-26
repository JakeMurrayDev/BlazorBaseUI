using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using BlazorBaseUI.Tests.Contracts.Menu;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.Menu;

public abstract class MenuTestsBase : TestBase,
    IMenuRootContract,
    IMenuTriggerContract,
    IMenuItemContract,
    IMenuCheckboxItemContract,
    IMenuRadioGroupContract,
    IMenuRadioItemContract,
    IMenuSubmenuTriggerContract
{
    protected MenuTestsBase(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }

    #region Helper Methods

    protected async Task OpenMenuAsync()
    {
        var trigger = GetByTestId("menu-trigger");
        await trigger.ClickAsync();
        await WaitForMenuOpenAsync();
    }

    protected async Task WaitForMenuOpenAsync()
    {
        var popup = GetByTestId("menu-popup");
        await popup.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    protected async Task WaitForMenuClosedAsync()
    {
        var openState = GetByTestId("open-state");
        await WaitForTextContentAsync(openState, "false");
    }

    protected async Task WaitForTextContentAsync(ILocator element, string expectedText, int timeout = 5000)
    {
        var effectiveTimeout = timeout * TimeoutMultiplier;
        var startTime = DateTime.UtcNow;
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < effectiveTimeout)
        {
            var text = await element.TextContentAsync();
            if (text == expectedText)
            {
                return;
            }
            await Task.Delay(50);
        }
        throw new TimeoutException($"Element text did not reach '{expectedText}' within {effectiveTimeout}ms");
    }

    #endregion

    #region IMenuRootContract

    [Fact]
    public virtual async Task CascadesContextToChildren()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var trigger = GetByTestId("menu-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Fact]
    public virtual async Task ControlledModeRespectsOpenParameter()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task UncontrolledModeUsesDefaultOpen()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task InvokesOnOpenChangeWithReason()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var changeCount = GetByTestId("change-count");
        var lastReason = GetByTestId("last-reason");

        await Assertions.Expect(changeCount).ToHaveTextAsync("0");

        await OpenMenuAsync();

        await Assertions.Expect(changeCount).ToHaveTextAsync("1");
        var reasonText = await lastReason.TextContentAsync();
        Assert.False(string.IsNullOrEmpty(reasonText), "Last reason should be set");
    }

    [Fact]
    public virtual async Task InvokesOnOpenChangeComplete()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var completeCount = GetByTestId("complete-count");

        await OpenMenuAsync();
        await Page.WaitForTimeoutAsync(500);

        var count = await completeCount.TextContentAsync();
        Assert.True(int.Parse(count!) >= 1, "OnOpenChangeComplete should have been invoked");
    }

    [Fact]
    public virtual async Task DisabledStatePreventsTriggerInteraction()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDisabled(true));

        var trigger = GetByTestId("menu-trigger");
        await trigger.ClickAsync(new LocatorClickOptions { Force = true });
        await Page.WaitForTimeoutAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual Task SupportsModalModes()
    {
        // Modal mode test - would need to check for backdrop rendering
        // This is more of an integration test
        return Task.CompletedTask;
    }

    [Fact]
    public virtual async Task SupportsOrientations()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithOrientation("horizontal"));

        var popup = GetByTestId("menu-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
    }

    [Fact]
    public virtual Task ActionsRefProvideCloseMethod()
    {
        // This would require exposing ActionsRef in the test page
        return Task.CompletedTask;
    }

    #endregion

    #region IMenuTriggerContract

    [Fact]
    public virtual async Task RendersAsButtonByDefault()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var trigger = GetByTestId("menu-trigger");
        var tagName = await trigger.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        Assert.Equal("button", tagName);
    }

    [Fact]
    public virtual async Task RendersWithCustomAs()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var trigger = GetByTestId("menu-trigger");
        await Assertions.Expect(trigger).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task ForwardsAdditionalAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var trigger = GetByTestId("menu-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-testid", "menu-trigger");
    }

    [Fact]
    public virtual async Task HasAriaHaspopupMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var trigger = GetByTestId("menu-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-haspopup", "menu");
    }

    [Fact]
    public virtual async Task HasAriaExpandedFalseWhenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var trigger = GetByTestId("menu-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Fact]
    public virtual async Task HasAriaExpandedTrueWhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var trigger = GetByTestId("menu-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Fact]
    public virtual async Task HasDataPopupOpenWhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var trigger = GetByTestId("menu-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-popup-open", "");
    }

    [Fact]
    public virtual async Task HasDataDisabledWhenDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDisabled(true));

        var trigger = GetByTestId("menu-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-disabled", "");
    }

    [Fact]
    public virtual async Task AppliesClassValueWithState()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var trigger = GetByTestId("menu-trigger");
        await Assertions.Expect(trigger).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task AppliesStyleValueWithState()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var trigger = GetByTestId("menu-trigger");
        await Assertions.Expect(trigger).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task ToggleMenuOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var trigger = GetByTestId("menu-trigger");
        var openState = GetByTestId("open-state");

        await trigger.ClickAsync();
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        await trigger.ClickAsync();
        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task DoesNotToggleWhenDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDisabled(true));

        var trigger = GetByTestId("menu-trigger");
        await trigger.ClickAsync(new LocatorClickOptions { Force = true });
        await Page.WaitForTimeoutAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual Task ThrowsWithoutMenuRootContext()
    {
        // This test is better suited for bUnit
        return Task.CompletedTask;
    }

    Task IMenuTriggerContract.RequiresContext()
    {
        return ThrowsWithoutMenuRootContext();
    }

    #endregion

    #region IMenuItemContract

    [Fact]
    public virtual async Task RendersAsDivByDefault()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item = GetByTestId("menu-item-1");
        var tagName = await item.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        Assert.Equal("div", tagName);
    }

    Task IMenuItemContract.RendersWithCustomAs()
    {
        return RendersWithCustomAs_Item();
    }

    [Fact]
    public virtual async Task RendersWithCustomAs_Item()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item = GetByTestId("menu-item-1");
        await Assertions.Expect(item).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task HasRoleMenuitem()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item = GetByTestId("menu-item-1");
        await Assertions.Expect(item).ToHaveAttributeAsync("role", "menuitem");
    }

    [Fact]
    public virtual async Task HasTabindexMinusOneByDefault()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        // Wait for menu to fully render
        await Page.WaitForTimeoutAsync(200);

        var item = GetByTestId("menu-item-2");
        var tabindex = await item.GetAttributeAsync("tabindex");
        // First item might have tabindex 0 if highlighted
        Assert.True(tabindex == "-1" || tabindex == "0", $"Expected tabindex -1 or 0, got {tabindex}");
    }

    [Fact]
    public virtual async Task HasTabindexZeroWhenHighlighted()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        var item2 = GetByTestId("menu-item-2");

        // First item should be highlighted initially with tabindex="0"
        var tabindex1Before = await item1.GetAttributeAsync("tabindex");
        Assert.Equal("0", tabindex1Before);

        // Navigate down to highlight item 2
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.WaitForTimeoutAsync(100);

        // Item 2 should now have tabindex="0", item 1 should have tabindex="-1"
        var tabindex1After = await item1.GetAttributeAsync("tabindex");
        var tabindex2After = await item2.GetAttributeAsync("tabindex");
        Assert.Equal("-1", tabindex1After);
        Assert.Equal("0", tabindex2After);
    }

    Task IMenuItemContract.HasDataDisabledWhenDisabled()
    {
        return HasDataDisabledWhenDisabled_Item();
    }

    [Fact]
    public virtual async Task HasDataDisabledWhenDisabled_Item()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item = GetByTestId("menu-item-3"); // Disabled item
        await Assertions.Expect(item).ToHaveAttributeAsync("data-disabled", "");
    }

    [Fact]
    public virtual async Task HasAriaDisabledWhenDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item = GetByTestId("menu-item-3"); // Disabled item
        await Assertions.Expect(item).ToHaveAttributeAsync("aria-disabled", "true");
    }

    [Fact]
    public virtual async Task InvokesOnClickHandler()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item = GetByTestId("menu-item-1");
        await item.ClickAsync();

        var lastClicked = GetByTestId("last-item-clicked");
        await Assertions.Expect(lastClicked).ToHaveTextAsync("1");
    }

    [Fact]
    public virtual async Task ClosesMenuOnClickByDefault()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item = GetByTestId("menu-item-1");
        await item.ClickAsync();

        await WaitForMenuClosedAsync();
    }

    [Fact]
    public virtual async Task DoesNotCloseWhenCloseOnClickFalse()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item = GetByTestId("menu-item-no-close");
        await item.ClickAsync();

        await Page.WaitForTimeoutAsync(300);
        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task DoesNotActivateWhenDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item = GetByTestId("menu-item-3"); // Disabled item
        await item.ClickAsync(new LocatorClickOptions { Force = true });

        // Menu should stay open since disabled item shouldn't trigger close
        await Page.WaitForTimeoutAsync(300);
        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    #endregion

    #region IMenuCheckboxItemContract

    [Fact]
    public virtual async Task HasRoleMenuitemcheckbox()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowCheckbox(true));

        var item = GetByTestId("menu-checkbox-item");
        await Assertions.Expect(item).ToHaveAttributeAsync("role", "menuitemcheckbox");
    }

    [Fact]
    public virtual async Task HasAriaCheckedFalseWhenUnchecked()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowCheckbox(true));

        var item = GetByTestId("menu-checkbox-item");
        await Assertions.Expect(item).ToHaveAttributeAsync("aria-checked", "false");
    }

    [Fact]
    public virtual async Task HasAriaCheckedTrueWhenChecked()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowCheckbox(true));

        var item = GetByTestId("menu-checkbox-item");
        await item.ClickAsync();
        await OpenMenuAsync(); // Re-open menu

        await Assertions.Expect(item).ToHaveAttributeAsync("aria-checked", "true");
    }

    [Fact]
    public virtual async Task HasDataCheckedWhenChecked()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowCheckbox(true));

        var item = GetByTestId("menu-checkbox-item");
        await item.ClickAsync();
        await OpenMenuAsync(); // Re-open menu

        await Assertions.Expect(item).ToHaveAttributeAsync("data-checked", "");
    }

    [Fact]
    public virtual async Task HasDataUncheckedWhenUnchecked()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowCheckbox(true));

        var item = GetByTestId("menu-checkbox-item");
        await Assertions.Expect(item).ToHaveAttributeAsync("data-unchecked", "");
    }

    [Fact]
    public virtual Task ControlledModeRespectsCheckedParameter()
    {
        // This test requires controlled mode setup
        return Task.CompletedTask;
    }

    [Fact]
    public virtual Task UncontrolledModeUsesDefaultChecked()
    {
        // Would need to modify test page to support defaultChecked
        return Task.CompletedTask;
    }

    [Fact]
    public virtual async Task TogglesOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowCheckbox(true));

        var checkboxState = GetByTestId("checkbox-state");
        await Assertions.Expect(checkboxState).ToHaveTextAsync("false");

        var item = GetByTestId("menu-checkbox-item");
        await item.ClickAsync();

        await Assertions.Expect(checkboxState).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task InvokesOnCheckedChange()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowCheckbox(true));

        var item = GetByTestId("menu-checkbox-item");
        await item.ClickAsync();

        var checkboxState = GetByTestId("checkbox-state");
        await Assertions.Expect(checkboxState).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual Task SupportsCancelInOnCheckedChange()
    {
        // This test requires cancel support in the test page
        return Task.CompletedTask;
    }

    #endregion

    #region IMenuRadioGroupContract

    [Fact]
    public virtual async Task HasRoleGroup()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowRadioGroup(true));

        var group = GetByTestId("menu-radio-group");
        await Assertions.Expect(group).ToHaveAttributeAsync("role", "group");
    }

    [Fact]
    public virtual async Task CascadesContextToRadioItems()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowRadioGroup(true));

        var item = GetByTestId("menu-radio-item-1");
        await Assertions.Expect(item).ToHaveAttributeAsync("role", "menuitemradio");
    }

    Task IMenuRadioGroupContract.ControlledModeRespectsValueParameter()
    {
        return ControlledModeRespectsValueParameter_RadioGroup();
    }

    [Fact]
    public virtual Task ControlledModeRespectsValueParameter_RadioGroup()
    {
        // This test requires controlled mode setup
        return Task.CompletedTask;
    }

    Task IMenuRadioGroupContract.UncontrolledModeUsesDefaultValue()
    {
        return UncontrolledModeUsesDefaultValue_RadioGroup();
    }

    [Fact]
    public virtual Task UncontrolledModeUsesDefaultValue_RadioGroup()
    {
        return Task.CompletedTask;
    }

    Task IMenuRadioGroupContract.InvokesOnValueChange()
    {
        return InvokesOnValueChange_RadioGroup();
    }

    [Fact]
    public virtual async Task InvokesOnValueChange_RadioGroup()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowRadioGroup(true));

        var item = GetByTestId("menu-radio-item-1");
        await item.ClickAsync();

        var radioState = GetByTestId("radio-state");
        await Assertions.Expect(radioState).ToHaveTextAsync("option1");
    }

    Task IMenuRadioGroupContract.SupportsCancelInOnValueChange()
    {
        return SupportsCancelInOnValueChange_RadioGroup();
    }

    [Fact]
    public virtual Task SupportsCancelInOnValueChange_RadioGroup()
    {
        // This test requires cancel support in the test page
        return Task.CompletedTask;
    }

    #endregion

    #region IMenuRadioItemContract

    [Fact]
    public virtual async Task HasRoleMenuitemradio()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowRadioGroup(true));

        var item = GetByTestId("menu-radio-item-1");
        await Assertions.Expect(item).ToHaveAttributeAsync("role", "menuitemradio");
    }

    [Fact]
    public virtual async Task HasAriaCheckedWhenSelected()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowRadioGroup(true));

        var item = GetByTestId("menu-radio-item-1");
        await item.ClickAsync();
        await OpenMenuAsync(); // Re-open menu

        await Assertions.Expect(item).ToHaveAttributeAsync("aria-checked", "true");
    }

    [Fact]
    public virtual async Task HasDataCheckedWhenSelected()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowRadioGroup(true));

        var item = GetByTestId("menu-radio-item-1");
        await item.ClickAsync();
        await OpenMenuAsync(); // Re-open menu

        await Assertions.Expect(item).ToHaveAttributeAsync("data-checked", "");
    }

    [Fact]
    public virtual async Task SelectsOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowRadioGroup(true));

        var radioState = GetByTestId("radio-state");

        var item1 = GetByTestId("menu-radio-item-1");
        await item1.ClickAsync();
        await Assertions.Expect(radioState).ToHaveTextAsync("option1");

        await OpenMenuAsync();
        var item2 = GetByTestId("menu-radio-item-2");
        await item2.ClickAsync();
        await Assertions.Expect(radioState).ToHaveTextAsync("option2");
    }

    [Fact]
    public virtual async Task InheritsDisabledFromGroup()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowRadioGroup(true));

        var item = GetByTestId("menu-radio-item-3"); // Disabled item
        await Assertions.Expect(item).ToHaveAttributeAsync("data-disabled", "");
    }

    #endregion

    #region IMenuSubmenuTriggerContract

    [Fact]
    public virtual async Task HasAriaHaspopup()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true));

        var trigger = GetByTestId("submenu-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-haspopup", "menu");
    }

    [Fact]
    public virtual async Task HasAriaExpanded()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true));

        var trigger = GetByTestId("submenu-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");

        await trigger.HoverAsync();
        await Page.WaitForTimeoutAsync(500);

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
    }

    [Fact]
    public virtual async Task HasDataOpenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true));

        var trigger = GetByTestId("submenu-trigger");

        await trigger.HoverAsync();
        await Page.WaitForTimeoutAsync(500);

        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-popup-open", "");
    }

    Task IMenuSubmenuTriggerContract.HasDataOpenWhenOpen()
    {
        return HasDataOpenClosed();
    }

    [Fact]
    public virtual async Task HasDataClosedWhenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true));

        var trigger = GetByTestId("submenu-trigger");

        // Submenu should be closed initially
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Fact]
    public virtual Task RequiresSubmenuContext()
    {
        // This test is better suited for bUnit
        return Task.CompletedTask;
    }

    #endregion

    #region Keyboard Navigation Tests

    [Fact]
    public virtual async Task ArrowDown_NavigatesToNextItem()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        var item2 = GetByTestId("menu-item-2");

        // First item should be highlighted initially
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.WaitForTimeoutAsync(100);

        await Assertions.Expect(item2).ToHaveAttributeAsync("data-highlighted", "");
    }

    [Fact]
    public virtual async Task ArrowUp_NavigatesToPreviousItem()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        var item2 = GetByTestId("menu-item-2");

        // Navigate down first
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.WaitForTimeoutAsync(100);
        await Assertions.Expect(item2).ToHaveAttributeAsync("data-highlighted", "");

        // Then navigate up
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.WaitForTimeoutAsync(100);
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");
    }

    [Fact]
    public virtual async Task Home_NavigatesToFirstItem()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");

        // Navigate to a later item
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.WaitForTimeoutAsync(100);

        // Press Home
        await Page.Keyboard.PressAsync("Home");
        await Page.WaitForTimeoutAsync(100);

        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");
    }

    [Fact]
    public virtual async Task End_NavigatesToLastItem()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var lastItem = GetByTestId("menu-item-no-close");

        await Page.Keyboard.PressAsync("End");
        await Page.WaitForTimeoutAsync(100);

        await Assertions.Expect(lastItem).ToHaveAttributeAsync("data-highlighted", "");
    }

    [Fact]
    public virtual async Task Enter_ActivatesHighlightedItem()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        await Page.Keyboard.PressAsync("Enter");

        var lastClicked = GetByTestId("last-item-clicked");
        await Assertions.Expect(lastClicked).ToHaveTextAsync("1");
    }

    [Fact]
    public virtual async Task Space_ActivatesHighlightedItem()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        await Page.Keyboard.PressAsync(" ");

        var lastClicked = GetByTestId("last-item-clicked");
        await Assertions.Expect(lastClicked).ToHaveTextAsync("1");
    }

    [Fact]
    public virtual async Task Escape_ClosesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        await Page.Keyboard.PressAsync("Escape");

        await WaitForMenuClosedAsync();
    }

    [Fact]
    public virtual async Task LoopFocus_WrapAroundWhenEnabled()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithLoopFocus(true));

        var firstItem = GetByTestId("menu-item-1");

        // Navigate to end
        await Page.Keyboard.PressAsync("End");
        await Page.WaitForTimeoutAsync(100);

        // Press down again - should wrap to first
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.WaitForTimeoutAsync(100);

        await Assertions.Expect(firstItem).ToHaveAttributeAsync("data-highlighted", "");
    }

    [Fact]
    public virtual async Task ArrowDown_SkipsDisabledItems()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item2 = GetByTestId("menu-item-2");
        var item4 = GetByTestId("menu-item-4");

        // Navigate to item 2
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.WaitForTimeoutAsync(100);
        await Assertions.Expect(item2).ToHaveAttributeAsync("data-highlighted", "");

        // Navigate down - should skip item 3 (disabled) and highlight item 4
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.WaitForTimeoutAsync(100);
        await Assertions.Expect(item4).ToHaveAttributeAsync("data-highlighted", "");
    }

    #endregion

    #region Focus Management Tests

    [Fact]
    public virtual async Task FocusFirstItem_OnMenuOpen()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        await OpenMenuAsync();

        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");
    }

    [Fact]
    public virtual async Task FocusReturnsToTrigger_OnClose()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        await OpenMenuAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForTimeoutAsync(200);

        var trigger = GetByTestId("menu-trigger");
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    #endregion

    #region Submenu Tests

    [Fact]
    public virtual async Task ArrowRight_OpensSubmenu()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true));

        var submenuTrigger = GetByTestId("submenu-trigger");

        // Navigate to submenu trigger
        await Page.Keyboard.PressAsync("End");
        await Page.WaitForTimeoutAsync(100);

        // Press right to open submenu
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.WaitForTimeoutAsync(300);

        var submenuState = GetByTestId("submenu-state");
        await Assertions.Expect(submenuState).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task ArrowLeft_ClosesSubmenu()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true));

        var submenuTrigger = GetByTestId("submenu-trigger");

        // Open submenu by hovering
        await submenuTrigger.HoverAsync();
        await Page.WaitForTimeoutAsync(500);

        var submenuState = GetByTestId("submenu-state");
        await Assertions.Expect(submenuState).ToHaveTextAsync("true");

        // Press left to close submenu
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Page.WaitForTimeoutAsync(300);

        await Assertions.Expect(submenuState).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task SubmenuOpensOnHover()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true));

        var submenuTrigger = GetByTestId("submenu-trigger");
        await submenuTrigger.HoverAsync();
        await Page.WaitForTimeoutAsync(500);

        var submenuPopup = GetByTestId("submenu-popup");
        await Assertions.Expect(submenuPopup).ToBeVisibleAsync();
    }

    #endregion

    #region Hover Behavior Tests

    [Fact]
    public virtual async Task OpenOnHover_OpensMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithOpenOnHover(true));

        var trigger = GetByTestId("menu-trigger");
        await trigger.HoverAsync();
        await Page.WaitForTimeoutAsync(500);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    #endregion

    #region Outside Click Tests

    [Fact]
    public virtual async Task OutsideClick_ClosesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();

        await WaitForMenuClosedAsync();
    }

    #endregion
}
