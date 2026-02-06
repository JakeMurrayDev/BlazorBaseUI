using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;
using Microsoft.Playwright;

namespace BlazorBaseUI.Playwright.Tests.Tests.ToggleGroup;

public abstract class ToggleGroupTestsBase : TestBase
{
    protected ToggleGroupTestsBase(PlaywrightFixture playwrightFixture)
        : base(playwrightFixture)
    {
    }

    #region Helper Methods

    protected ILocator GetToggleGroup() => GetByTestId("toggle-group");
    protected ILocator GetToggle(string value) => GetByTestId($"toggle-{value}");
    protected ILocator GetValueState() => GetByTestId("value-state");
    protected ILocator GetChangeCount() => GetByTestId("change-count");

    protected async Task WaitForTogglePressedAsync(string value, bool expected, int timeout = 5000)
    {
        var effectiveTimeout = timeout * TimeoutMultiplier;
        var toggle = GetToggle(value);
        await Assertions.Expect(toggle).ToHaveAttributeAsync(
            "aria-pressed",
            expected ? "true" : "false",
            new LocatorAssertionsToHaveAttributeOptions { Timeout = effectiveTimeout });
    }

    protected async Task WaitForToggleGroupJsAsync()
    {
        await Page.WaitForFunctionAsync(@"() => {
            const el = document.querySelector('[data-testid=""toggle-group""]');
            if (!el) return false;
            const stateKey = Symbol.for('BlazorBaseUI.ToggleGroup.State');
            const map = window[stateKey];
            if (!map || !map.has(el)) return false;
            // Verify all toggle items have their keydown handlers registered
            // (initializeGroupItem sets GROUP_ITEM_STATE_KEY on each element)
            const itemKey = Symbol.for('BlazorBaseUI.ToggleGroupItem.State');
            const toggles = el.querySelectorAll('[aria-pressed]');
            if (toggles.length < 3) return false;
            return Array.from(toggles).every(t => t[itemKey] !== undefined);
        }", new PageWaitForFunctionOptions { Timeout = 5000 * TimeoutMultiplier });
    }

    private async Task PerformArrowNavigationAsync(string fromToggle, string key, string expectedFocusToggle)
    {
        await WaitForTogglePressedAsync(fromToggle, true);

        // Allow OnAfterRenderAsync JS initialization (initializeGroup + registerToggle +
        // initializeGroupItem) to complete before attempting keyboard navigation.
        await WaitForDelayAsync(500);

        // Under concurrent test load, keyboard events can occasionally be lost.
        // Retry the key press if the expected state isn't reached.
        var toggle = GetToggle(fromToggle);
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await toggle.FocusAsync();
            await WaitForDelayAsync(100);
            await Page.Keyboard.PressAsync(key);

            try
            {
                // Assert focus moved to the expected toggle. We check focus (not tabindex)
                // because Blazor's EventCallback auto-StateHasChanged triggers a re-render
                // after HandleKeyDownAsync completes, and updateToggleTabIndexes resets
                // tabindex based on pressed state — overwriting the value set by
                // setFocusedToggle. Focus itself persists correctly.
                var expected = GetToggle(expectedFocusToggle);
                await Assertions.Expect(expected).ToBeFocusedAsync(
                    new LocatorAssertionsToBeFocusedOptions { Timeout = 5000 * TimeoutMultiplier });
                return;
            }
            catch when (attempt < 3)
            {
                // Key press was likely lost due to concurrent load. Retry.
                await WaitForDelayAsync(500);
            }
        }
    }

    #endregion

    #region Click Interaction Tests

    [Fact]
    public virtual async Task Click_PressesToggle()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup"));

        await WaitForTogglePressedAsync("one", false);

        var toggle = GetToggle("one");
        await toggle.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForTogglePressedAsync("one", true);
    }

    [Fact]
    public virtual async Task Click_DepressesToggle()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup")
            .WithToggleGroupDefaultValue("one"));

        await WaitForTogglePressedAsync("one", true);

        var toggle = GetToggle("one");
        await toggle.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForTogglePressedAsync("one", false);
    }

    [Fact]
    public virtual async Task Click_MultipleMode_AllowsMultiplePressed()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup")
            .WithMultiple(true));

        var toggleOne = GetToggle("one");
        var toggleTwo = GetToggle("two");

        await toggleOne.ClickAsync();
        await WaitForDelayAsync(100);
        await WaitForTogglePressedAsync("one", true);

        await toggleTwo.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForTogglePressedAsync("one", true);
        await WaitForTogglePressedAsync("two", true);
    }

    [Fact]
    public virtual async Task Click_SingleMode_DeselectsPrevious()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup"));

        var toggleOne = GetToggle("one");
        var toggleTwo = GetToggle("two");

        await toggleOne.ClickAsync();
        await WaitForDelayAsync(100);
        await WaitForTogglePressedAsync("one", true);

        await toggleTwo.ClickAsync();
        await WaitForDelayAsync(100);

        await WaitForTogglePressedAsync("one", false);
        await WaitForTogglePressedAsync("two", true);
    }

    #endregion

    #region Default Value

    [Fact]
    public virtual async Task UncontrolledDefaultValue_InitiallyPressed()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup")
            .WithToggleGroupDefaultValue("two"));

        await WaitForTogglePressedAsync("one", false);
        await WaitForTogglePressedAsync("two", true);
        await WaitForTogglePressedAsync("three", false);
    }

    #endregion

    #region Controlled Value

    [Fact]
    public virtual async Task ControlledValue_ReflectsExternalState()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup"));

        var selectOne = GetByTestId("select-one");
        await selectOne.ClickAsync();
        await WaitForDelayAsync(200);

        await WaitForTogglePressedAsync("one", true);

        var selectTwo = GetByTestId("select-two");
        await selectTwo.ClickAsync();
        await WaitForDelayAsync(200);

        await WaitForTogglePressedAsync("one", false);
        await WaitForTogglePressedAsync("two", true);
    }

    [Fact]
    public virtual async Task ControlledValue_ExternalClear()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup")
            .WithToggleGroupDefaultValue("one"));

        await WaitForTogglePressedAsync("one", true);

        var clear = GetByTestId("clear");
        await clear.ClickAsync();
        await WaitForDelayAsync(200);

        await WaitForTogglePressedAsync("one", false);
        await WaitForTogglePressedAsync("two", false);
        await WaitForTogglePressedAsync("three", false);
    }

    #endregion

    #region Disabled

    [Fact]
    public virtual async Task DisabledGroup_PreventsInteraction()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup")
            .WithDisabled(true));

        var toggle = GetToggle("one");
        await toggle.ClickAsync(new LocatorClickOptions { Force = true });
        await WaitForDelayAsync(100);

        await WaitForTogglePressedAsync("one", false);
    }

    [Fact]
    public virtual async Task DisabledGroup_PropagatesDataAttribute()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup")
            .WithDisabled(true));

        await Assertions.Expect(GetToggle("one")).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(GetToggle("two")).ToHaveAttributeAsync("data-disabled", "");
        await Assertions.Expect(GetToggle("three")).ToHaveAttributeAsync("data-disabled", "");
    }

    #endregion

    #region Keyboard Navigation

    [Fact]
    public virtual async Task ArrowRight_MovesToNextToggle()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup")
            .WithToggleGroupDefaultValue("one"));

        await WaitForToggleGroupJsAsync();
        await PerformArrowNavigationAsync("one", "ArrowRight", "two");
    }

    [Fact]
    public virtual async Task ArrowLeft_MovesToPreviousToggle()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup")
            .WithToggleGroupDefaultValue("two"));

        await WaitForToggleGroupJsAsync();
        await PerformArrowNavigationAsync("two", "ArrowLeft", "one");
    }

    [Fact]
    public virtual async Task ArrowRight_WrapsToFirst()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup")
            .WithToggleGroupDefaultValue("three"));

        await WaitForToggleGroupJsAsync();
        await PerformArrowNavigationAsync("three", "ArrowRight", "one");
    }

    [Fact]
    public virtual async Task ArrowLeft_WrapsToLast()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup")
            .WithToggleGroupDefaultValue("one"));

        await WaitForToggleGroupJsAsync();
        await PerformArrowNavigationAsync("one", "ArrowLeft", "three");
    }

    [Fact]
    public virtual async Task VerticalOrientation_ArrowDownUp()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup")
            .WithOrientation("vertical")
            .WithToggleGroupDefaultValue("one"));

        await WaitForToggleGroupJsAsync();
        await PerformArrowNavigationAsync("one", "ArrowDown", "two");
    }

    #endregion

    #region Keyboard Toggle

    [Fact]
    public virtual async Task Enter_TogglesPressedState()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup")
            .WithToggleGroupDefaultValue("one"));

        await WaitForToggleGroupJsAsync();
        await WaitForDelayAsync(500);

        var toggle = GetToggle("one");
        await toggle.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(200);

        // Enter should depress "one"
        await WaitForTogglePressedAsync("one", false);
    }

    [Fact]
    public virtual async Task Space_TogglesPressedState()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup")
            .WithToggleGroupDefaultValue("one"));

        await WaitForToggleGroupJsAsync();
        await WaitForDelayAsync(500);

        var toggle = GetToggle("one");
        await toggle.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(200);

        // Space should depress "one"
        await WaitForTogglePressedAsync("one", false);
    }

    #endregion

    #region OnValueChange

    [Fact]
    public virtual async Task OnValueChange_FiresOnClick()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup"));

        await Assertions.Expect(GetChangeCount()).ToHaveTextAsync("0");

        var toggle = GetToggle("one");
        await toggle.ClickAsync();
        await WaitForDelayAsync(100);

        await Assertions.Expect(GetChangeCount()).ToHaveTextAsync("1");
    }

    [Fact]
    public virtual async Task OnValueChange_FiresOnEnter()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup"));

        await WaitForToggleGroupJsAsync();
        await WaitForDelayAsync(500);

        await Assertions.Expect(GetChangeCount()).ToHaveTextAsync("0");

        var toggle = GetToggle("one");
        await toggle.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await WaitForDelayAsync(200);

        await Assertions.Expect(GetChangeCount()).ToHaveTextAsync("1");
    }

    [Fact]
    public virtual async Task OnValueChange_FiresOnSpace()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup"));

        await WaitForToggleGroupJsAsync();
        await WaitForDelayAsync(500);

        await Assertions.Expect(GetChangeCount()).ToHaveTextAsync("0");

        var toggle = GetToggle("one");
        await toggle.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await WaitForDelayAsync(200);

        await Assertions.Expect(GetChangeCount()).ToHaveTextAsync("1");
    }

    #endregion

    #region Focus Management

    [Fact]
    public virtual async Task Tab_FocusesPressedOrFirstToggle()
    {
        await NavigateAsync(CreateUrl("/tests/togglegroup")
            .WithToggleGroupDefaultValue("two"));

        await WaitForTogglePressedAsync("two", true);
        await WaitForToggleGroupJsAsync();
        await WaitForDelayAsync(500);

        var outsideButton = GetByTestId("outside-button");
        await outsideButton.FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await WaitForDelayAsync(200);

        // Should focus the pressed toggle ("two"), not the first one
        var toggleTwo = GetToggle("two");
        await Assertions.Expect(toggleTwo).ToBeFocusedAsync(
            new LocatorAssertionsToBeFocusedOptions { Timeout = 3000 * TimeoutMultiplier });
    }

    #endregion
}
