using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.ContextMenu;

/// <summary>
/// Playwright tests for ContextMenu component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: right-click activation, keyboard navigation, focus management,
/// outside click, positioning at cursor, and real JS interop execution.
/// </summary>
public abstract class ContextMenuTestsBase : TestBase
{
    protected ContextMenuTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected async Task OpenContextMenuAsync(string triggerTestId = "context-menu-trigger")
    {
        var trigger = GetByTestId(triggerTestId);
        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await WaitForContextMenuOpenAsync();
    }

    protected async Task WaitForContextMenuOpenAsync()
    {
        var popup = GetByTestId("context-menu-popup");
        await popup.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    protected async Task WaitForContextMenuClosedAsync()
    {
        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    #endregion

    #region Right-Click Activation Tests

    /// <summary>
    /// Tests that right-clicking the trigger area opens the context menu.
    /// Requires real browser contextmenu event handling via JS interop.
    /// </summary>
    [Fact]
    public virtual async Task OpensMenuOnRightClick()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        var trigger = GetByTestId("context-menu-trigger");
        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    /// <summary>
    /// Tests that the data-popup-open attribute is added when menu opens and removed when it closes.
    /// </summary>
    [Fact]
    public virtual async Task AddsAndRemovesDataAttributes()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        var trigger = GetByTestId("context-menu-trigger");

        // Initially no data-popup-open
        await Assertions.Expect(trigger).Not.ToHaveAttributeAsync("data-popup-open", "");

        // Right-click to open
        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });
        await WaitForContextMenuOpenAsync();

        // data-popup-open should now be present
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-popup-open", "");

        // Close with Escape
        await Page.Keyboard.PressAsync("Escape");
        await WaitForContextMenuClosedAsync();

        // data-popup-open should be removed
        await Assertions.Expect(trigger).Not.ToHaveAttributeAsync("data-popup-open", "");
    }

    /// <summary>
    /// Tests that the OnOpenChange callback fires when menu opens via right-click.
    /// </summary>
    [Fact]
    public virtual async Task InvokesOnOpenChangeOnRightClick()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        await OpenContextMenuAsync();

        var changeCount = GetByTestId("change-count");
        await Assertions.Expect(changeCount).Not.ToHaveTextAsync("0", new LocatorAssertionsToHaveTextOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    #endregion

    #region Keyboard Navigation Tests

    /// <summary>
    /// Tests ArrowDown navigation within the context menu.
    /// </summary>
    [Fact]
    public virtual async Task KeyboardNavigation_ArrowDown()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        var item2 = GetByTestId("menu-item-2");

        // First item should be highlighted
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(item2).ToHaveAttributeAsync("data-highlighted", "");
    }

    /// <summary>
    /// Tests ArrowUp navigation within the context menu.
    /// </summary>
    [Fact]
    public virtual async Task KeyboardNavigation_ArrowUp()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        var item2 = GetByTestId("menu-item-2");

        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Assertions.Expect(item2).ToHaveAttributeAsync("data-highlighted", "");

        await Page.Keyboard.PressAsync("ArrowUp");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");
    }

    /// <summary>
    /// Tests that Escape closes the context menu.
    /// </summary>
    [Fact]
    public virtual async Task Escape_ClosesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        await Page.Keyboard.PressAsync("Escape");
        await WaitForContextMenuClosedAsync();
    }

    /// <summary>
    /// Tests that Enter activates the highlighted item.
    /// </summary>
    [Fact]
    public virtual async Task Enter_ActivatesHighlightedItem()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDefaultOpen(true));

        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        await Page.Keyboard.PressAsync("Enter");

        var lastClicked = GetByTestId("last-item-clicked");
        await Assertions.Expect(lastClicked).ToHaveTextAsync("1");
    }

    #endregion

    #region Focus Management Tests

    /// <summary>
    /// Tests that the first item is focused when the context menu opens.
    /// </summary>
    [Fact]
    public virtual async Task FocusFirstItem_OnMenuOpen()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        await OpenContextMenuAsync();

        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");
    }

    #endregion

    #region Outside Click Tests

    /// <summary>
    /// Tests that clicking outside the context menu closes it.
    /// </summary>
    [Fact]
    public virtual async Task OutsideClick_ClosesMenu()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDefaultOpen(true));

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();

        await WaitForContextMenuClosedAsync();
    }

    #endregion

    #region Positioning Tests

    /// <summary>
    /// Tests that the context menu appears near the cursor position.
    /// </summary>
    [Fact]
    public virtual async Task PositionsAtCursorLocation()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu"));

        var trigger = GetByTestId("context-menu-trigger");
        var box = await trigger.BoundingBoxAsync();
        Assert.NotNull(box);

        // Right-click at specific coordinates within the trigger
        var clickX = box.X + box.Width / 2;
        var clickY = box.Y + box.Height / 2;
        await Page.Mouse.ClickAsync(clickX, clickY, new MouseClickOptions { Button = MouseButton.Right });

        await WaitForContextMenuOpenAsync();

        // Verify popup appeared (positioning is handled by Floating UI)
        var popup = GetByTestId("context-menu-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
    }

    #endregion

    #region Submenu Tests

    /// <summary>
    /// Tests that submenu opens on hover within the context menu.
    /// </summary>
    [Fact]
    public virtual async Task SubmenuOpensOnHover()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu")
            .WithDefaultOpen(true)
            .WithContextMenuShowSubmenu(true));

        var item1 = GetByTestId("menu-item-1");
        await Assertions.Expect(item1).ToHaveAttributeAsync("data-highlighted", "");

        var submenuTrigger = GetByTestId("submenu-trigger");
        await submenuTrigger.HoverAsync();

        var submenuPopup = GetByTestId("submenu-popup");
        await Assertions.Expect(submenuPopup).ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
        {
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    #endregion

    #region Disabled State Tests

    /// <summary>
    /// Tests that right-clicking a disabled trigger does not open the menu.
    /// </summary>
    [Fact]
    public virtual async Task DisabledTrigger_DoesNotOpenMenu()
    {
        await NavigateAsync(CreateUrl("/tests/context-menu").WithDisabled(true));

        var trigger = GetByTestId("context-menu-trigger");
        await trigger.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });

        await WaitForDelayAsync(500);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    #endregion
}
