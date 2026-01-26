using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using BlazorBaseUI.Tests.Contracts.MenuBar;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.MenuBar;

public abstract class MenuBarTestsBase : TestBase,
    IMenuBarRootContract
{
    protected MenuBarTestsBase(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }

    #region Helper Methods

    protected async Task OpenMenu1Async()
    {
        var trigger = GetByTestId("menu-1-trigger");
        await trigger.ClickAsync();
        await WaitForMenu1OpenAsync();
    }

    protected async Task WaitForMenu1OpenAsync()
    {
        var popup = GetByTestId("menu-1-popup");
        await popup.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });
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

    #region IMenuBarRootContract

    [Fact]
    public virtual async Task RendersAsDivByDefault()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var root = GetByTestId("menubar-root");
        var tagName = await root.EvaluateAsync<string>("el => el.tagName.toLowerCase()");
        Assert.Equal("div", tagName);
    }

    [Fact]
    public virtual async Task HasRoleMenubar()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var root = GetByTestId("menubar-root");
        await Assertions.Expect(root).ToHaveAttributeAsync("role", "menubar");
    }

    [Fact]
    public virtual async Task HasAriaOrientationHorizontalByDefault()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var root = GetByTestId("menubar-root");
        await Assertions.Expect(root).ToHaveAttributeAsync("aria-orientation", "horizontal");
    }

    [Fact]
    public virtual async Task HasAriaOrientationVerticalWhenSet()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithOrientation("vertical"));

        var root = GetByTestId("menubar-root");
        await Assertions.Expect(root).ToHaveAttributeAsync("aria-orientation", "vertical");
    }

    [Fact]
    public virtual async Task HasDataOrientationAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var root = GetByTestId("menubar-root");
        await Assertions.Expect(root).ToHaveAttributeAsync("data-orientation", "horizontal");
    }

    [Fact]
    public virtual async Task RendersWithCustomAs()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var root = GetByTestId("menubar-root");
        await Assertions.Expect(root).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task HasDataDisabledWhenDisabled()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithDisabled(true));

        var root = GetByTestId("menubar-root");
        await Assertions.Expect(root).ToHaveAttributeAsync("data-disabled", "");
    }

    [Fact]
    public virtual async Task CascadesContextToChildren()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var trigger1 = GetByTestId("menu-1-trigger");
        var trigger2 = GetByTestId("menu-2-trigger");
        var trigger3 = GetByTestId("menu-3-trigger");

        await Assertions.Expect(trigger1).ToBeVisibleAsync();
        await Assertions.Expect(trigger2).ToBeVisibleAsync();
        await Assertions.Expect(trigger3).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task TracksHasSubmenuOpenState()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var root = GetByTestId("menubar-root");

        // Initially no submenu is open
        var hasAttr = await root.EvaluateAsync<bool>("el => el.hasAttribute('data-has-submenu-open')");
        Assert.False(hasAttr);

        // Open a menu
        await OpenMenu1Async();

        // data-has-submenu-open should be present
        await Assertions.Expect(root).ToHaveAttributeAsync("data-has-submenu-open", "");
    }

    #endregion

    #region Keyboard Navigation Tests

    [Fact]
    public virtual async Task ArrowRight_NavigatesToNextMenubarItem()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var trigger1 = GetByTestId("menu-1-trigger");
        var trigger2 = GetByTestId("menu-2-trigger");

        // Focus and click on first trigger
        await trigger1.ClickAsync();
        await WaitForMenu1OpenAsync();

        // Press right to navigate to next menu
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.WaitForTimeoutAsync(300);

        // Second menu should now be open
        var menu2State = GetByTestId("menu-2-state");
        await Assertions.Expect(menu2State).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task ArrowLeft_NavigatesToPreviousMenubarItem()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var trigger2 = GetByTestId("menu-2-trigger");

        // Click on second trigger
        await trigger2.ClickAsync();
        var popup2 = GetByTestId("menu-2-popup");
        await popup2.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });

        // Press left to navigate to previous menu
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Page.WaitForTimeoutAsync(300);

        // First menu should now be open
        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task HoverOpensSibling_WhenOneIsOpen()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        // Click to open first menu
        await OpenMenu1Async();

        // Hover over second trigger
        var trigger2 = GetByTestId("menu-2-trigger");
        await trigger2.HoverAsync();
        await Page.WaitForTimeoutAsync(300);

        // Second menu should open
        var menu2State = GetByTestId("menu-2-state");
        await Assertions.Expect(menu2State).ToHaveTextAsync("true");

        // First menu should close
        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task HoverDoesNotOpen_WhenNoneOpen()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        // Hover over first trigger without clicking
        var trigger1 = GetByTestId("menu-1-trigger");
        await trigger1.HoverAsync();
        await Page.WaitForTimeoutAsync(300);

        // Menu should NOT open
        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task MenuBar_ClickTrigger_OpensMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        var trigger1 = GetByTestId("menu-1-trigger");
        await trigger1.ClickAsync();

        await WaitForMenu1OpenAsync();

        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task MenuBar_ClickItem_ClosesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        await OpenMenu1Async();

        var item1 = GetByTestId("menu-1-item-1");
        await item1.ClickAsync();

        await Page.WaitForTimeoutAsync(300);

        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("false");

        var lastClicked = GetByTestId("last-item-clicked");
        await Assertions.Expect(lastClicked).ToHaveTextAsync("Menu1-Item1");
    }

    [Fact]
    public virtual async Task MenuBar_Escape_ClosesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        await OpenMenu1Async();

        await Page.Keyboard.PressAsync("Escape");
        await Page.WaitForTimeoutAsync(300);

        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task MenuBar_ClickOutside_ClosesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        await OpenMenu1Async();

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();

        await Page.WaitForTimeoutAsync(300);

        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task MenuBar_ArrowDown_NavigatesInMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar"));

        await OpenMenu1Async();

        var item1 = GetByTestId("menu-1-item-1");
        var item2 = GetByTestId("menu-1-item-2");

        // Press ArrowDown to highlight first item (menu opens with no item highlighted)
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.WaitForTimeoutAsync(100);

        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        // Press ArrowDown again to navigate to second item
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.WaitForTimeoutAsync(100);

        await Assertions.Expect(item2).ToHaveAttributeAsync("data-highlighted", "");
    }

    [Fact]
    public virtual async Task MenuBar_LoopFocus_WrapAround()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithLoopFocus(true));

        var trigger1 = GetByTestId("menu-1-trigger");
        var trigger3 = GetByTestId("menu-3-trigger");

        await trigger3.ClickAsync();
        var popup3 = GetByTestId("menu-3-popup");
        await popup3.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });

        // Navigate right from last menu (should wrap to first)
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.WaitForTimeoutAsync(300);

        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task MenuBar_Disabled_PreventsInteraction()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithDisabled(true));

        var trigger1 = GetByTestId("menu-1-trigger");
        await trigger1.ClickAsync(new LocatorClickOptions { Force = true });
        await Page.WaitForTimeoutAsync(300);

        var menu1State = GetByTestId("menu-1-state");
        await Assertions.Expect(menu1State).ToHaveTextAsync("false");
    }

    #endregion

    #region Submenu Tests

    [Fact]
    public virtual async Task MenuBar_SubmenuOpensOnHover()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithShowSubmenu(true));

        // Open menu 4
        var trigger4 = GetByTestId("menu-4-trigger");
        await trigger4.ClickAsync();
        var popup4 = GetByTestId("menu-4-popup");
        await popup4.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });

        // Hover over submenu trigger
        var submenuTrigger = GetByTestId("menu-4-submenu-trigger");
        await submenuTrigger.HoverAsync();
        await Page.WaitForTimeoutAsync(500);

        var submenuState = GetByTestId("submenu-state");
        await Assertions.Expect(submenuState).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task MenuBar_ArrowRight_OpensSubmenu()
    {
        await NavigateAsync(CreateUrl("/tests/menubar").WithShowSubmenu(true));

        // Open menu 4
        var trigger4 = GetByTestId("menu-4-trigger");
        await trigger4.ClickAsync();
        var popup4 = GetByTestId("menu-4-popup");
        await popup4.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });

        // Navigate to submenu trigger (menu opens with no item highlighted)
        // First ArrowDown highlights Documentation (index 0)
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.WaitForTimeoutAsync(100);
        // Second ArrowDown highlights More Help submenu trigger (index 1)
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.WaitForTimeoutAsync(100);

        // Open submenu with arrow right
        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.WaitForTimeoutAsync(300);

        var submenuState = GetByTestId("submenu-state");
        await Assertions.Expect(submenuState).ToHaveTextAsync("true");
    }

    #endregion
}
