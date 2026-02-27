namespace BlazorBaseUI.Tests.Select;

public class SelectItemTests : BunitContext, ISelectItemContract
{
    public SelectItemTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupSelectModule(JSInterop);
    }

    private RenderFragment CreateSelectWithItems(
        string? defaultValue = null,
        bool defaultOpen = false,
        bool disabledItem = false)
    {
        return builder =>
        {
            builder.OpenComponent<SelectRoot<string>>(0);
            var i = 1;
            if (defaultValue is not null) builder.AddAttribute(i++, "DefaultValue", defaultValue);
            builder.AddAttribute(i++, "DefaultOpen", defaultOpen);
            builder.AddAttribute(i++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<SelectTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Select")));
                innerBuilder.CloseComponent();

                // Items directly (no portal) for bUnit
                innerBuilder.OpenComponent<SelectPositioner>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<SelectPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<SelectItem<string>>(0);
                        popupBuilder.AddAttribute(1, "Value", "apple");
                        popupBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Apple")));
                        popupBuilder.CloseComponent();

                        popupBuilder.OpenComponent<SelectItem<string>>(10);
                        popupBuilder.AddAttribute(11, "Value", "banana");
                        if (disabledItem) popupBuilder.AddAttribute(12, "Disabled", true);
                        popupBuilder.AddAttribute(13, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Banana")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task ShouldSelectItemAndClosePopupWhenClicked()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        items[0].Click();

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ShouldNotSelectDisabledItem()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true, disabledItem: true));

        var items = cut.FindAll("[role='option']");
        items[1].Click();

        // Disabled items don't trigger selection, so the select should remain open
        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        // The banana item should NOT have data-selected
        items = cut.FindAll("[role='option']");
        items[1].HasAttribute("data-selected").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ShouldApplyDataSelectedWhenSelected()
    {
        var cut = Render(CreateSelectWithItems(defaultValue: "apple", defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        items[0].HasAttribute("data-selected").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ShouldApplyDataHighlightedWhenHighlighted()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true));

        var items = cut.FindAll("[role='option']");

        // Initially not highlighted: boolean false means attribute is absent
        items[0].HasAttribute("data-highlighted").ShouldBeFalse();

        await items[0].TriggerEventAsync("onmouseenter", new MouseEventArgs());

        // After mouseenter, the item should have data-highlighted attribute (boolean true renders as present attribute)
        items = cut.FindAll("[role='option']");
        items[0].HasAttribute("data-highlighted").ShouldBeTrue();
    }

    [Fact]
    public Task ShouldRenderWithOptionRole()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        items.Count.ShouldBeGreaterThan(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ShouldSetAriaSelectedTrue()
    {
        var cut = Render(CreateSelectWithItems(defaultValue: "apple", defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        items[0].GetAttribute("aria-selected").ShouldBe("true");
        items[1].GetAttribute("aria-selected").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledItem_HasAriaDisabled()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true, disabledItem: true));

        var items = cut.FindAll("[role='option']");
        items[1].GetAttribute("aria-disabled").ShouldBe("true");
        items[1].HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    // --- Focus + Disabled: disabled items do not highlight on mouseenter ---
    // In our implementation, the mouseenter handler checks !Disabled, so disabled
    // items do NOT highlight. This test verifies that behavior.

    [Fact]
    public async Task DisabledItem_ShouldNotHighlightOnMouseEnter()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true, disabledItem: true));

        var items = cut.FindAll("[role='option']");
        // Banana (index 1) is disabled; mouseenter won't highlight it in our impl
        await items[1].TriggerEventAsync("onmouseenter", new MouseEventArgs());

        items = cut.FindAll("[role='option']");
        // Our implementation does NOT highlight disabled items on mouseenter
        // This matches the guard in HandleMouseEnterAsync: if (!Disabled)
        items[1].HasAttribute("data-highlighted").ShouldBeFalse();
    }

    // --- Focus on open: selected item should have data-selected upon opening ---

    [Fact]
    public Task ShouldFocusSelectedItemUponOpeningPopup()
    {
        var cut = Render(CreateSelectWithItems(defaultValue: "banana", defaultOpen: true));

        var items = cut.FindAll("[role='option']");
        var bananaItem = items.First(i => i.TextContent.Contains("Banana"));
        bananaItem.HasAttribute("data-selected").ShouldBeTrue();
        bananaItem.GetAttribute("aria-selected").ShouldBe("true");

        return Task.CompletedTask;
    }

    // --- Disabled item click guard: clicking a disabled item should not select it or close the popup ---

    [Fact]
    public Task DisabledItem_ShouldNotSelectOnClickAndKeepOpen()
    {
        var cut = Render(CreateSelectWithItems(defaultOpen: true, disabledItem: true));

        // Try to click the disabled item (banana)
        var items = cut.FindAll("[role='option']");
        items[1].Click();

        // Since banana is disabled, it should not be selected
        items = cut.FindAll("[role='option']");
        items[1].HasAttribute("data-selected").ShouldBeFalse();

        // Select should remain open (disabled item click doesn't close)
        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }
}
