using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.Menu;

/// <summary>
/// Playwright tests for Menu component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: keyboard navigation, focus management, hover interactions,
/// outside click, text navigation, RTL support, and real JS interop execution.
/// </summary>
public abstract class MenuTestsBase : TestBase
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
        await Assertions.Expect(element).ToHaveTextAsync(expectedText, new LocatorAssertionsToHaveTextOptions
        {
            Timeout = effectiveTimeout
        });
    }

    #endregion

    #region Menu Open/Close Interaction Tests

    /// <summary>
    /// Tests that clicking the trigger toggles the menu open/closed state.
    /// Requires real browser to test JS interop for positioning and state sync.
    /// </summary>
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

    #endregion

    #region Menu Item Interaction Tests

    /// <summary>
    /// Tests that tabindex changes dynamically based on keyboard navigation highlighting.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task HasTabindexZeroWhenHighlighted()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        var item2 = GetByTestId("menu-item-2");

        // First item should be highlighted initially with tabindex="0"
        await Assertions.Expect(item1).ToHaveAttributeAsync("tabindex", "0");

        // Navigate down to highlight item 2
        await Page.Keyboard.PressAsync("ArrowDown");

        // Item 2 should now have tabindex="0", item 1 should have tabindex="-1"
        await Assertions.Expect(item1).ToHaveAttributeAsync("tabindex", "-1");
        await Assertions.Expect(item2).ToHaveAttributeAsync("tabindex", "0");
    }

    #endregion

    #region Submenu Hover Interaction Tests

    /// <summary>
    /// Tests that submenu trigger's aria-expanded updates based on hover state.
    /// Requires real browser hover events.
    /// </summary>
    [Fact]
    public virtual async Task SubmenuTrigger_HasAriaExpanded_OnHover()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true));

        var trigger = GetByTestId("submenu-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");

        await trigger.HoverAsync();
        await WaitForDelayAsync(500);

        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
    }

    /// <summary>
    /// Tests that submenu trigger's data-popup-open attribute is set on hover.
    /// Requires real browser hover events.
    /// </summary>
    [Fact]
    public virtual async Task SubmenuTrigger_HasDataPopupOpen_OnHover()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true));

        var trigger = GetByTestId("submenu-trigger");

        await trigger.HoverAsync();
        await WaitForDelayAsync(500);

        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-popup-open", "");
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
        await WaitForDelayAsync(100);

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
        await WaitForDelayAsync(100);
        await Assertions.Expect(item2).ToHaveAttributeAsync("data-highlighted", "");

        // Then navigate up
        await Page.Keyboard.PressAsync("ArrowUp");
        await WaitForDelayAsync(100);
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
        await WaitForDelayAsync(100);

        // Press Home
        await Page.Keyboard.PressAsync("Home");
        await WaitForDelayAsync(100);

        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");
    }

    [Fact]
    public virtual async Task End_NavigatesToLastItem()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithDefaultOpen(true));

        var lastItem = GetByTestId("menu-item-no-close");

        await Page.Keyboard.PressAsync("End");
        await WaitForDelayAsync(100);

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
        await WaitForDelayAsync(100);

        // Press down again - should wrap to first
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForDelayAsync(100);

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
        await WaitForDelayAsync(100);
        await Assertions.Expect(item2).ToHaveAttributeAsync("data-highlighted", "");

        // Navigate down - should skip item 3 (disabled) and highlight item 4
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForDelayAsync(100);
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
        await WaitForDelayAsync(200);

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

        // Wait for menu to be fully initialized (first item highlighted)
        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        // Navigate to submenu trigger
        await Page.Keyboard.PressAsync("End");
        await WaitForDelayAsync(100);

        // Verify submenu trigger is highlighted
        await Assertions.Expect(submenuTrigger).ToHaveAttributeAsync("data-highlighted", "");

        // Press right to open submenu
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(300);

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
        await WaitForDelayAsync(500);

        var submenuState = GetByTestId("submenu-state");
        await Assertions.Expect(submenuState).ToHaveTextAsync("true");

        // Press left to close submenu
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(300);

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
        await WaitForDelayAsync(500);

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
        await WaitForDelayAsync(500);

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

    #region Keyboard Trigger Tests

    [Fact]
    public virtual async Task Enter_OpensMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var trigger = GetByTestId("menu-trigger");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await WaitForMenuOpenAsync();
    }

    [Fact]
    public virtual async Task Space_OpensMenu()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var trigger = GetByTestId("menu-trigger");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync(" ");

        await WaitForMenuOpenAsync();
    }

    [Fact]
    public virtual async Task ArrowDown_OpensMenuAndFocusesFirstItem()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var trigger = GetByTestId("menu-trigger");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await WaitForMenuOpenAsync();

        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");
    }

    [Fact]
    public virtual async Task ArrowUp_OpensMenuAndFocusesLastItem()
    {
        await NavigateAsync(CreateUrl("/tests/menu"));

        var trigger = GetByTestId("menu-trigger");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await WaitForMenuOpenAsync();

        // Last item should be highlighted (menu-item-no-close is the last item)
        var lastItem = GetByTestId("menu-item-no-close");
        await Assertions.Expect(lastItem).ToHaveAttributeAsync("data-highlighted", "");
    }

    #endregion

    #region CloseDelay Tests

    [Fact]
    public virtual async Task CloseDelay_ClosesMenuAfterDelay()
    {
        // Use a longer close delay to ensure the menu doesn't close immediately
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithOpenOnHover(true)
            .WithCloseDelay(1500));

        // Allow JS hover interaction to initialize (needed for Server mode)
        await WaitForDelayAsync(500);

        var trigger = GetByTestId("menu-trigger");
        await trigger.HoverAsync();
        // Wait for hover to be processed and menu to open
        await WaitForDelayAsync(500);

        // Verify menu is open
        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Move mouse away from trigger - this starts the close delay timer
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.HoverAsync();

        // Immediately verify menu is still open (close delay should prevent instant close)
        var immediateState = await openState.TextContentAsync();
        Assert.Equal("true", immediateState);

        // Wait for close delay to pass and verify menu closes
        await Assertions.Expect(openState).ToHaveTextAsync("false", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 3000 * TimeoutMultiplier
        });
    }

    #endregion

    #region Nested Submenu Tests

    [Fact]
    public virtual async Task NestedSubmenu_OpensOnHover()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true)
            .WithShowNestedSubmenu(true));

        // Open first submenu
        var submenuTrigger = GetByTestId("submenu-trigger");
        await submenuTrigger.HoverAsync();
        await WaitForDelayAsync(500);

        var submenuPopup = GetByTestId("submenu-popup");
        await Assertions.Expect(submenuPopup).ToBeVisibleAsync();

        // Open nested submenu
        var nestedTrigger = GetByTestId("nested-submenu-trigger");
        await nestedTrigger.HoverAsync();
        await WaitForDelayAsync(500);

        var nestedPopup = GetByTestId("nested-submenu-popup");
        await Assertions.Expect(nestedPopup).ToBeVisibleAsync();
    }

    [Fact]
    public virtual async Task NestedSubmenu_ClosesEntireTree_OnOutsideClick()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true)
            .WithShowNestedSubmenu(true));

        // Open submenu chain
        var submenuTrigger = GetByTestId("submenu-trigger");
        await submenuTrigger.HoverAsync();
        await WaitForDelayAsync(500);

        var nestedTrigger = GetByTestId("nested-submenu-trigger");
        await nestedTrigger.HoverAsync();
        await WaitForDelayAsync(500);

        // Click outside
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();

        // All menus should be closed
        await WaitForMenuClosedAsync();
    }

    [Fact]
    public virtual async Task KeepsParentSubmenuOpen_AfterNestedSubmenuCloses()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true)
            .WithShowNestedSubmenu(true));

        // Open submenu chain
        var submenuTrigger = GetByTestId("submenu-trigger");
        await submenuTrigger.HoverAsync();
        await WaitForDelayAsync(500);

        var nestedTrigger = GetByTestId("nested-submenu-trigger");
        await nestedTrigger.HoverAsync();
        await WaitForDelayAsync(500);

        var nestedSubmenuState = GetByTestId("nested-submenu-state");
        await Assertions.Expect(nestedSubmenuState).ToHaveTextAsync("true");

        // Move mouse back to submenu item (not nested)
        var submenuItem = GetByTestId("submenu-item-1");
        await submenuItem.HoverAsync();
        await WaitForDelayAsync(300);

        // Nested submenu should be closed
        await Assertions.Expect(nestedSubmenuState).ToHaveTextAsync("false");

        // But parent submenu should still be open
        var submenuState = GetByTestId("submenu-state");
        await Assertions.Expect(submenuState).ToHaveTextAsync("true");
    }

    #endregion

    #region Text Navigation Tests

    [Fact]
    public virtual async Task TextNavigation_JumpsToItemStartingWithTypedCharacter()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowTextNavItems(true));

        // Wait for first item to be highlighted (indicates menu is fully initialized)
        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        // Type 'b' to jump to "Banana"
        await Page.Keyboard.PressAsync("b");
        await WaitForDelayAsync(100);

        var bananaItem = GetByTestId("menu-item-banana");
        await Assertions.Expect(bananaItem).ToHaveAttributeAsync("data-highlighted", "");
    }

    [Fact]
    public virtual async Task TextNavigation_UsesLabelAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowTextNavItems(true));

        // Wait for first item to be highlighted (indicates menu is fully initialized)
        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        // Type 'c' to jump to "Cherry" using its label attribute
        await Page.Keyboard.PressAsync("c");
        await WaitForDelayAsync(100);

        var cherryItem = GetByTestId("menu-item-cherry");
        await Assertions.Expect(cherryItem).ToHaveAttributeAsync("data-highlighted", "");
    }

    [Fact]
    public virtual async Task TextNavigation_WrapsAroundList()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowTextNavItems(true));

        // Wait for first item to be highlighted (indicates menu is fully initialized)
        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        // Jump to Apple first
        await Page.Keyboard.PressAsync("a");
        await WaitForDelayAsync(100);

        var appleItem = GetByTestId("menu-item-apple");
        await Assertions.Expect(appleItem).ToHaveAttributeAsync("data-highlighted", "");

        // Type 'a' again - should wrap and find "Apricot" (next item starting with 'a')
        await Page.Keyboard.PressAsync("a");
        await WaitForDelayAsync(100);

        var apricotItem = GetByTestId("menu-item-apricot");
        await Assertions.Expect(apricotItem).ToHaveAttributeAsync("data-highlighted", "");
    }

    [Fact]
    public virtual async Task TextNavigation_IgnoresModifierKeys()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowTextNavItems(true));

        // Wait for first item to be highlighted (indicates menu is fully initialized)
        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        // Ctrl+B should not navigate (browser shortcut, not typeahead)
        await Page.Keyboard.PressAsync("Control+b");
        await WaitForDelayAsync(100);

        // First item should still be highlighted
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");
    }

    #endregion

    #region CloseParentOnEsc Tests

    [Fact]
    public virtual async Task Escape_DoesNotCloseParentMenu_ByDefault()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true)
            .WithCloseParentOnEsc(false));

        // Wait for menu to be fully initialized (first item highlighted)
        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        // Navigate to submenu trigger using keyboard and open it
        var submenuTrigger = GetByTestId("submenu-trigger");
        await Page.Keyboard.PressAsync("End");
        await Assertions.Expect(submenuTrigger).ToHaveAttributeAsync("data-highlighted", "");

        // Open submenu with ArrowRight
        await Page.Keyboard.PressAsync("ArrowRight");

        var submenuState = GetByTestId("submenu-state");
        await Assertions.Expect(submenuState).ToHaveTextAsync("true");

        // Press Escape - should close submenu only, not parent
        await Page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(submenuState).ToHaveTextAsync("false");

        // Parent menu should still be open
        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task Escape_ClosesParentMenu_WhenCloseParentOnEscTrue()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true)
            .WithCloseParentOnEsc(true));

        // Wait for menu to be fully initialized (first item highlighted)
        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        // Navigate to submenu trigger using keyboard and open it
        var submenuTrigger = GetByTestId("submenu-trigger");
        await Page.Keyboard.PressAsync("End");
        await Assertions.Expect(submenuTrigger).ToHaveAttributeAsync("data-highlighted", "");

        // Open submenu with ArrowRight
        await Page.Keyboard.PressAsync("ArrowRight");

        var submenuState = GetByTestId("submenu-state");
        await Assertions.Expect(submenuState).ToHaveTextAsync("true");

        // Press Escape - should close BOTH submenu and parent
        await Page.Keyboard.PressAsync("Escape");

        // Both should be closed
        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    #endregion

    #region Modal Tests

    [Fact]
    public virtual async Task Modal_RendersBackdrop()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithModal(true));

        await OpenMenuAsync();

        var popup = GetByTestId("menu-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        // Verify backdrop is rendered with role="presentation"
        // The internal backdrop is rendered as a sibling before the positioner
        var backdrop = Page.Locator("[role='presentation']");
        await Assertions.Expect(backdrop).ToBeAttachedAsync();
    }

    [Fact]
    public virtual async Task Modal_PreventsScrolling()
    {
        await NavigateAsync(CreateUrl("/tests/menu").WithModal(true));

        await OpenMenuAsync();

        var popup = GetByTestId("menu-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        // Verify scroll lock is applied - check for data-base-ui-scroll-locked attribute
        // or overflow: hidden on document element or body
        var isScrollLocked = await Page.EvaluateAsync<bool>(@"() => {
            const doc = document.documentElement;
            const body = document.body;
            return doc.hasAttribute('data-base-ui-scroll-locked') ||
                   doc.style.overflow === 'hidden' ||
                   body.style.overflow === 'hidden';
        }");
        Assert.True(isScrollLocked, "Scroll should be locked when modal menu is open");
    }

    #endregion

    #region HighlightItemOnHover Tests

    [Fact]
    public virtual async Task HighlightsItemOnMouseMove()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithHighlightItemOnHover(true));

        var item1 = GetByTestId("menu-item-1");
        var item2 = GetByTestId("menu-item-2");

        // Move mouse over item 2
        await item2.HoverAsync();
        await WaitForDelayAsync(100);

        // Item 2 should be highlighted
        await Assertions.Expect(item2).ToHaveAttributeAsync("data-highlighted", "");
    }

    [Fact]
    public virtual async Task DoesNotHighlightOnHover_WhenHighlightItemOnHoverFalse()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithHighlightItemOnHover(false));

        var item1 = GetByTestId("menu-item-1");
        var item2 = GetByTestId("menu-item-2");

        // Item 1 should be initially highlighted (keyboard navigation)
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        // Move mouse over item 2
        await item2.HoverAsync();

        // Item 2 should NOT be highlighted when highlightItemOnHover is false
        await Assertions.Expect(item2).Not.ToHaveAttributeAsync("data-highlighted", "");

        // Item 1 should still be highlighted
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");
    }

    #endregion

    #region RTL Support Tests

    [Fact]
    public virtual async Task RTL_ArrowRightClosesSubmenu()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true)
            .WithDirection("rtl"));

        // Open submenu
        var submenuTrigger = GetByTestId("submenu-trigger");
        await submenuTrigger.HoverAsync();
        await WaitForDelayAsync(500);

        var submenuState = GetByTestId("submenu-state");
        await Assertions.Expect(submenuState).ToHaveTextAsync("true");

        // In RTL, ArrowRight should close submenu (opposite of LTR)
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(300);

        await Assertions.Expect(submenuState).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task RTL_ArrowLeftOpensSubmenu()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true)
            .WithDirection("rtl"));

        // Navigate to submenu trigger
        await Page.Keyboard.PressAsync("End");
        await WaitForDelayAsync(100);

        var submenuTrigger = GetByTestId("submenu-trigger");
        await Assertions.Expect(submenuTrigger).ToHaveAttributeAsync("data-highlighted", "");

        // In RTL, ArrowLeft should open submenu (opposite of LTR)
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(300);

        var submenuState = GetByTestId("submenu-state");
        await Assertions.Expect(submenuState).ToHaveTextAsync("true");
    }

    #endregion

    #region Horizontal Orientation Tests

    [Fact]
    public virtual async Task HorizontalOrientation_ArrowLeftRight_NavigatesItems()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithOrientation("horizontal"));

        var item1 = GetByTestId("menu-item-1");
        var item2 = GetByTestId("menu-item-2");

        // First item should be highlighted initially
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        // ArrowRight should navigate to next item
        await Page.Keyboard.PressAsync("ArrowRight");
        await WaitForDelayAsync(100);

        await Assertions.Expect(item2).ToHaveAttributeAsync("data-highlighted", "");

        // ArrowLeft should navigate back
        await Page.Keyboard.PressAsync("ArrowLeft");
        await WaitForDelayAsync(100);

        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");
    }

    #endregion

    #region Hover Behavior with OpenOnHover Tests

    [Fact]
    public virtual async Task OpenOnHover_ClosesMenuWhenTriggerNoLongerHovered()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithOpenOnHover(true)
            .WithCloseDelay(100));

        // Allow JS hover interaction to initialize (needed for Server mode)
        await WaitForDelayAsync(500);

        var trigger = GetByTestId("menu-trigger");
        await trigger.HoverAsync();
        // Wait for hover to be processed and menu to open
        await WaitForDelayAsync(500);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Move away from trigger
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.HoverAsync();
        await WaitForDelayAsync(300);

        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    [Fact]
    public virtual async Task OpenOnHover_DoesNotCloseWhenHoveringPopup()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithOpenOnHover(true)
            .WithCloseDelay(100));

        // Allow JS hover interaction to initialize (needed for Server mode)
        await WaitForDelayAsync(500);

        var trigger = GetByTestId("menu-trigger");
        await trigger.HoverAsync();
        // Wait for hover to be processed and menu to open
        await WaitForDelayAsync(500);

        // Verify menu is open
        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Move to popup
        var popup = GetByTestId("menu-popup");
        await popup.HoverAsync();
        await WaitForDelayAsync(300);

        // Menu should still be open
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    [Fact]
    public virtual async Task SubmenuOpensWithZeroDelay_WhenParentOpenOnHover()
    {
        await NavigateAsync(CreateUrl("/tests/menu")
            .WithDefaultOpen(true)
            .WithShowSubmenu(true));

        var submenuTrigger = GetByTestId("submenu-trigger");
        await submenuTrigger.HoverAsync();

        // Submenu should open quickly
        var submenuPopup = GetByTestId("submenu-popup");
        await Assertions.Expect(submenuPopup).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
        {
            Timeout = 1000 * TimeoutMultiplier
        });
    }

    #endregion
}
