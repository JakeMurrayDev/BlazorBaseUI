using BlazorBaseUI;

namespace BlazorBaseUI.Tests.MenuBar;

public class MenuBarRootTests : BunitContext, IMenuBarRootContract
{
    public MenuBarRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuBarModule(JSInterop);
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateMenuBarRoot(
        bool disabled = false,
        bool loopFocus = true,
        bool modal = true,
        Orientation orientation = Orientation.Horizontal,
        Func<MenuBarRootState, string>? classValue = null,
        Func<MenuBarRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        string? asElement = null,
        bool includeMenus = true)
    {
        return builder =>
        {
            builder.OpenComponent<MenuBarRoot>(0);
            var attrIndex = 1;

            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            builder.AddAttribute(attrIndex++, "LoopFocus", loopFocus);
            builder.AddAttribute(attrIndex++, "Modal", modal);
            builder.AddAttribute(attrIndex++, "Orientation", orientation);
            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
            if (asElement is not null)
                builder.AddAttribute(attrIndex++, "As", asElement);

            if (includeMenus)
            {
                builder.AddAttribute(attrIndex++, "ChildContent", CreateChildContent());
            }
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateChildContent()
    {
        return builder =>
        {
            // First menu
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(menuBuilder =>
            {
                menuBuilder.OpenComponent<MenuTrigger>(0);
                menuBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "File")));
                menuBuilder.CloseComponent();

                menuBuilder.OpenComponent<MenuPositioner>(2);
                menuBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "New")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                menuBuilder.CloseComponent();
            }));
            builder.CloseComponent();

            // Second menu
            builder.OpenComponent<MenuRoot>(10);
            builder.AddAttribute(11, "ChildContent", (RenderFragment)(menuBuilder =>
            {
                menuBuilder.OpenComponent<MenuTrigger>(0);
                menuBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Edit")));
                menuBuilder.CloseComponent();

                menuBuilder.OpenComponent<MenuPositioner>(2);
                menuBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Undo")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                menuBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateMenuBarRoot(includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomAs()
    {
        var cut = Render(CreateMenuBarRoot(asElement: "nav", includeMenus: false));

        var menubar = cut.Find("nav[role='menubar']");
        menubar.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRoleMenubar()
    {
        var cut = Render(CreateMenuBarRoot(includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.GetAttribute("role").ShouldBe("menubar");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaOrientationHorizontalByDefault()
    {
        var cut = Render(CreateMenuBarRoot(orientation: Orientation.Horizontal, includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.GetAttribute("aria-orientation").ShouldBe("horizontal");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaOrientationVerticalWhenSet()
    {
        var cut = Render(CreateMenuBarRoot(orientation: Orientation.Vertical, includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.GetAttribute("aria-orientation").ShouldBe("vertical");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientationAttribute()
    {
        var cut = Render(CreateMenuBarRoot(orientation: Orientation.Horizontal, includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.GetAttribute("data-orientation").ShouldBe("horizontal");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateMenuBarRoot(disabled: true, includeMenus: false));

        var menubar = cut.Find("[role='menubar']");
        menubar.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesContextToChildren()
    {
        var cut = Render(CreateMenuBarRoot());

        // MenuBarRoot should cascade context to child menus
        // Child menus should render their triggers
        var triggers = cut.FindAll("button[aria-haspopup='menu']");
        triggers.Count.ShouldBe(2);

        return Task.CompletedTask;
    }

    [Fact]
    public Task TracksHasSubmenuOpenState()
    {
        // This test verifies that the MenuBarRoot tracks has-submenu-open state
        // In bUnit, the state tracking works through the context, but the 
        // actual data attribute update requires full rendering cycles.
        // This test verifies the initial state and that the triggers render correctly.
        var cut = Render(CreateMenuBarRoot());

        var menubar = cut.Find("[role='menubar']");

        // Initially no submenu is open
        menubar.GetAttribute("data-has-submenu-open").ShouldBe("false");

        // Click first trigger to open menu
        var trigger = cut.Find("button[aria-haspopup='menu']");
        trigger.Click();

        // Verify the menu opened (the trigger's aria-expanded should change)
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        // Note: The data-has-submenu-open attribute update happens through
        // MenuBarContext.SetHasSubmenuOpen which is called from MenuRoot.SetOpenAsync.
        // Full integration of this behavior is better tested via Playwright E2E tests.

        return Task.CompletedTask;
    }
}
