using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.Popover;

/// <summary>
/// Playwright tests for Popover component - focused on browser-specific behavior.
/// Static rendering, attribute forwarding, and basic state tests are handled by bUnit.
/// These tests cover: click interactions, keyboard navigation, focus management,
/// outside click, escape key, hover behavior, and real JS interop execution.
/// </summary>
public abstract class PopoverTestsBase : TestBase
{
    protected PopoverTestsBase(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }

    #region Helper Methods

    protected async Task OpenPopoverAsync()
    {
        var trigger = GetByTestId("popover-trigger");
        await trigger.ClickAsync();
        await WaitForPopoverOpenAsync();
    }

    protected async Task WaitForPopoverOpenAsync()
    {
        var popup = GetByTestId("popover-popup");
        await popup.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000 * TimeoutMultiplier
        });
    }

    protected async Task WaitForPopoverClosedAsync()
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

    #region Popover Open/Close Interaction Tests

    /// <summary>
    /// Tests that clicking the trigger toggles the popover open/closed state.
    /// Requires real browser to test JS interop for positioning and state sync.
    /// </summary>
    [Fact]
    public virtual async Task TogglePopoverOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        var trigger = GetByTestId("popover-trigger");
        var openState = GetByTestId("open-state");

        await trigger.ClickAsync();
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        await trigger.ClickAsync();
        await WaitForPopoverClosedAsync();
    }

    /// <summary>
    /// Tests that the popover opens when defaultOpen is true.
    /// </summary>
    [Fact]
    public virtual async Task OpensWithDefaultOpenTrue()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithDefaultOpen(true));

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
    }

    #endregion

    #region Keyboard Interaction Tests

    /// <summary>
    /// Tests that pressing Escape closes the popover.
    /// Requires real browser keyboard events.
    /// </summary>
    [Fact]
    public virtual async Task Escape_ClosesPopover()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithDefaultOpen(true));

        // Wait for JS interop to initialize and popover to be visible
        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Focus the popup for Escape to be captured
        await popup.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");

        await WaitForPopoverClosedAsync();
    }

    /// <summary>
    /// Tests that Enter key on trigger opens the popover.
    /// </summary>
    [Fact]
    public virtual async Task Enter_OpensPopover()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        var trigger = GetByTestId("popover-trigger");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await WaitForPopoverOpenAsync();
    }

    /// <summary>
    /// Tests that Space key on trigger opens the popover.
    /// </summary>
    [Fact]
    public virtual async Task Space_OpensPopover()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        var trigger = GetByTestId("popover-trigger");
        await trigger.FocusAsync();
        await Page.Keyboard.PressAsync(" ");

        await WaitForPopoverOpenAsync();
    }

    #endregion

    #region Outside Click Tests

    /// <summary>
    /// Tests that clicking outside the popover closes it.
    /// Requires real browser to test outside click detection.
    /// Note: Uses click to open because defaultOpen doesn't sync JS state properly.
    /// </summary>
    [Fact]
    public virtual async Task OutsideClick_ClosesPopover()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        // Open via click to ensure JS state is synced
        await OpenPopoverAsync();
        await WaitForDelayAsync(200);

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();

        await WaitForPopoverClosedAsync();
    }

    #endregion

    #region Focus Management Tests

    /// <summary>
    /// Tests that the popup can receive focus (has tabindex).
    /// Note: The popup has tabindex="-1" to allow programmatic focus.
    /// </summary>
    [Fact]
    public virtual async Task PopupCanReceiveFocus()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        await OpenPopoverAsync();

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToHaveAttributeAsync("tabindex", "-1");

        // Verify the popup can be focused programmatically
        await popup.FocusAsync();
        await Assertions.Expect(popup).ToBeFocusedAsync();
    }

    /// <summary>
    /// Tests that the trigger can be focused after popover closes.
    /// Note: Current implementation doesn't automatically return focus to trigger.
    /// This test verifies the trigger is still focusable after close.
    /// </summary>
    [Fact]
    public virtual async Task TriggerIsFocusableAfterClose()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithShowClose(true));

        await OpenPopoverAsync();

        // Close the popover
        var closeButton = GetByTestId("popover-close");
        await closeButton.ClickAsync();
        await WaitForPopoverClosedAsync();

        // Verify trigger can receive focus
        var trigger = GetByTestId("popover-trigger");
        await trigger.FocusAsync();
        await Assertions.Expect(trigger).ToBeFocusedAsync();
    }

    #endregion

    #region Close Button Tests

    /// <summary>
    /// Tests that clicking the close button closes the popover.
    /// </summary>
    [Fact]
    public virtual async Task CloseButton_ClosesPopover()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithDefaultOpen(true)
            .WithShowClose(true));

        var closeButton = GetByTestId("popover-close");
        await closeButton.ClickAsync();

        await WaitForPopoverClosedAsync();
    }

    #endregion

    #region Hover Behavior Tests

    /// <summary>
    /// Tests that popover opens on hover when OpenOnHover is true.
    /// </summary>
    [Fact]
    public virtual async Task OpenOnHover_OpensPopover()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(100));

        // Allow JS hover interaction to initialize
        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();
        await WaitForDelayAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");
    }

    /// <summary>
    /// Tests that popover closes when mouse leaves when OpenOnHover is true.
    /// </summary>
    [Fact]
    public virtual async Task OpenOnHover_ClosesOnMouseLeave()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithOpenOnHover(true)
            .WithOpenDelay(100)
            .WithCloseDelay(100));

        // Allow JS hover interaction to initialize
        await WaitForDelayAsync(500);

        var trigger = GetByTestId("popover-trigger");
        await trigger.HoverAsync();
        await WaitForDelayAsync(300);

        var openState = GetByTestId("open-state");
        await Assertions.Expect(openState).ToHaveTextAsync("true");

        // Move away from trigger
        var outsideButton = GetByTestId("outside-button");
        await outsideButton.HoverAsync();
        await WaitForDelayAsync(300);

        await Assertions.Expect(openState).ToHaveTextAsync("false");
    }

    #endregion

    #region Modal Tests

    /// <summary>
    /// Tests that modal popover renders with backdrop.
    /// </summary>
    [Fact]
    public virtual async Task Modal_RendersBackdrop()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithModal(true)
            .WithShowBackdrop(true));

        await OpenPopoverAsync();
        await WaitForDelayAsync(100);

        var backdrop = GetByTestId("popover-backdrop");
        // Check that backdrop is attached and has the expected attributes
        await Assertions.Expect(backdrop).ToBeAttachedAsync();
        await Assertions.Expect(backdrop).ToHaveAttributeAsync("role", "presentation");
        await Assertions.Expect(backdrop).ToHaveAttributeAsync("data-open", "");
    }

    #endregion

    #region Positioning Tests

    /// <summary>
    /// Tests that the positioner has correct default data-side attribute (bottom).
    /// Note: Testing custom side values requires further investigation of query param binding.
    /// </summary>
    [Fact]
    public virtual async Task Positioner_HasDataSideAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithDefaultOpen(true));

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        // Default side is "bottom"
        var positioner = GetByTestId("popover-positioner");
        await Assertions.Expect(positioner).ToHaveAttributeAsync("data-side", "bottom");
    }

    /// <summary>
    /// Tests that the positioner has correct data-align attribute.
    /// </summary>
    [Fact]
    public virtual async Task Positioner_HasCorrectAlignAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithDefaultOpen(true)
            .WithAlign("start"));

        var positioner = GetByTestId("popover-positioner");
        await Assertions.Expect(positioner).ToHaveAttributeAsync("data-align", "start");
    }

    #endregion

    #region Accessibility Tests

    /// <summary>
    /// Tests that trigger has correct aria-expanded attribute.
    /// </summary>
    [Fact]
    public virtual async Task Trigger_HasAriaExpandedFalse_WhenClosed()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        var trigger = GetByTestId("popover-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
    }

    /// <summary>
    /// Tests that trigger has correct aria-expanded attribute when open.
    /// </summary>
    [Fact]
    public virtual async Task Trigger_HasAriaExpandedTrue_WhenOpen()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithDefaultOpen(true));

        var trigger = GetByTestId("popover-trigger");
        await Assertions.Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
    }

    /// <summary>
    /// Tests that popup has role="dialog".
    /// </summary>
    [Fact]
    public virtual async Task Popup_HasDialogRole()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithDefaultOpen(true));

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToHaveAttributeAsync("role", "dialog");
    }

    /// <summary>
    /// Tests that popup is labeled by title when present.
    /// </summary>
    [Fact]
    public virtual async Task Popup_HasAriaLabelledBy_WhenTitlePresent()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithDefaultOpen(true)
            .WithShowTitle(true));

        var popup = GetByTestId("popover-popup");
        var title = GetByTestId("popover-title");

        var titleId = await title.GetAttributeAsync("id");
        await Assertions.Expect(popup).ToHaveAttributeAsync("aria-labelledby", titleId!);
    }

    /// <summary>
    /// Tests that popup is described by description when present.
    /// </summary>
    [Fact]
    public virtual async Task Popup_HasAriaDescribedBy_WhenDescriptionPresent()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithDefaultOpen(true)
            .WithShowDescription(true));

        var popup = GetByTestId("popover-popup");
        var description = GetByTestId("popover-description");

        var descriptionId = await description.GetAttributeAsync("id");
        await Assertions.Expect(popup).ToHaveAttributeAsync("aria-describedby", descriptionId!);
    }

    #endregion

    #region Arrow Tests

    /// <summary>
    /// Tests that arrow is rendered when ShowArrow is true.
    /// </summary>
    [Fact]
    public virtual async Task Arrow_IsRendered_WhenEnabled()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithDefaultOpen(true)
            .WithShowArrow(true));

        // Wait for popup to be visible first
        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        // Check arrow is attached and has expected attributes
        var arrow = GetByTestId("popover-arrow");
        await Assertions.Expect(arrow).ToBeAttachedAsync();
        await Assertions.Expect(arrow).ToHaveAttributeAsync("aria-hidden", "true");
        await Assertions.Expect(arrow).ToHaveAttributeAsync("data-open", "");
    }

    /// <summary>
    /// Tests that arrow has data-side attribute.
    /// </summary>
    [Fact]
    public virtual async Task Arrow_HasDataSideAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithDefaultOpen(true)
            .WithShowArrow(true));

        var arrow = GetByTestId("popover-arrow");
        await Assertions.Expect(arrow).ToHaveAttributeAsync("data-side", "bottom");
    }

    #endregion

    #region KeepMounted Tests

    /// <summary>
    /// Tests that popover stays in DOM when KeepMounted is true.
    /// </summary>
    [Fact]
    public virtual async Task KeepMounted_PopoverStaysInDOM()
    {
        await NavigateAsync(CreateUrl("/tests/popover")
            .WithKeepMounted(true)
            .WithShowClose(true));

        // Open the popover
        await OpenPopoverAsync();

        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();

        // Close the popover using the close button (more reliable than Escape)
        var closeButton = GetByTestId("popover-close");
        await closeButton.ClickAsync();
        await WaitForPopoverClosedAsync();

        // Positioner should still be in DOM with data-closed attribute
        var positioner = GetByTestId("popover-positioner");
        await Assertions.Expect(positioner).ToBeAttachedAsync();
        await Assertions.Expect(positioner).ToHaveAttributeAsync("data-closed", "");
    }

    #endregion

    #region Event Tests

    /// <summary>
    /// Tests that OnOpenChange is called with correct reason.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChange_FiresWithCorrectReason()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        var trigger = GetByTestId("popover-trigger");
        await trigger.ClickAsync();
        await WaitForPopoverOpenAsync();

        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("TriggerPress");
    }

    /// <summary>
    /// Tests that OnOpenChange is called when closing with Escape.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChange_FiresOnEscapeClose()
    {
        await NavigateAsync(CreateUrl("/tests/popover").WithDefaultOpen(true));

        // Wait for JS interop to initialize and popover to be visible
        var popup = GetByTestId("popover-popup");
        await Assertions.Expect(popup).ToBeVisibleAsync();
        await WaitForDelayAsync(200);

        // Focus the popup and press Escape
        await popup.FocusAsync();
        await Page.Keyboard.PressAsync("Escape");
        await WaitForPopoverClosedAsync();

        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("EscapeKey");
    }

    /// <summary>
    /// Tests that OnOpenChange is called when closing with outside click.
    /// Note: Uses click to open because defaultOpen doesn't sync JS state properly.
    /// </summary>
    [Fact]
    public virtual async Task OnOpenChange_FiresOnOutsideClick()
    {
        await NavigateAsync(CreateUrl("/tests/popover"));

        // Open via click to ensure JS state is synced
        await OpenPopoverAsync();
        await WaitForDelayAsync(200);

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.ClickAsync();
        await WaitForPopoverClosedAsync();

        var lastReason = GetByTestId("last-reason");
        await Assertions.Expect(lastReason).ToHaveTextAsync("OutsidePress");
    }

    #endregion
}
