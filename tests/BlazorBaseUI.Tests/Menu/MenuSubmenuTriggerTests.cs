namespace BlazorBaseUI.Tests.Menu;

public class MenuSubmenuTriggerTests : BunitContext, IMenuSubmenuTriggerContract
{
    public MenuSubmenuTriggerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateSubmenuTriggerInRoot(
        bool parentDefaultOpen = true,
        bool submenuDefaultOpen = false,
        bool triggerDisabled = false,
        string? asElement = null)
    {
        return builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", parentDefaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Main Trigger")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<MenuPositioner>(2);
                innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item 1")));
                        popupBuilder.CloseComponent();

                        popupBuilder.OpenComponent<MenuSubmenuRoot>(2);
                        popupBuilder.AddAttribute(3, "DefaultOpen", submenuDefaultOpen);
                        popupBuilder.AddAttribute(4, "ChildContent", (RenderFragment)(submenuBuilder =>
                        {
                            submenuBuilder.OpenComponent<MenuSubmenuTrigger>(0);
                            var attrIndex = 1;

                            if (triggerDisabled)
                                submenuBuilder.AddAttribute(attrIndex++, "Disabled", true);
                            if (asElement is not null)
                                submenuBuilder.AddAttribute(attrIndex++, "As", asElement);
                            submenuBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Submenu")));
                            submenuBuilder.CloseComponent();

                            submenuBuilder.OpenComponent<MenuPositioner>(10);
                            submenuBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(subPosBuilder =>
                            {
                                subPosBuilder.OpenComponent<MenuPopup>(0);
                                subPosBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(subPopupBuilder =>
                                {
                                    subPopupBuilder.OpenComponent<MenuItem>(0);
                                    subPopupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Sub Item 1")));
                                    subPopupBuilder.CloseComponent();
                                }));
                                subPosBuilder.CloseComponent();
                            }));
                            submenuBuilder.CloseComponent();
                        }));
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
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateSubmenuTriggerInRoot());

        // Find submenu trigger by looking for element with both role=menuitem and aria-haspopup
        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomAs()
    {
        var cut = Render(CreateSubmenuTriggerInRoot(asElement: "span"));

        var submenuTrigger = cut.Find("span[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHaspopupMenu()
    {
        var cut = Render(CreateSubmenuTriggerInRoot());

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup]");
        submenuTrigger.GetAttribute("aria-haspopup").ShouldBe("menu");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedFalseWhenClosed()
    {
        var cut = Render(CreateSubmenuTriggerInRoot(submenuDefaultOpen: false));

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedTrueWhenOpen()
    {
        var cut = Render(CreateSubmenuTriggerInRoot(submenuDefaultOpen: true));

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateSubmenuTriggerInRoot(submenuDefaultOpen: true));

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreateSubmenuTriggerInRoot(submenuDefaultOpen: false));

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.HasAttribute("data-closed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateSubmenuTriggerInRoot(triggerDisabled: true));

        var submenuTrigger = cut.Find("[role='menuitem'][aria-haspopup='menu']");
        submenuTrigger.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresSubmenuContext()
    {
        // MenuSubmenuTrigger throws when not inside a MenuSubmenuRoot
        Should.Throw<InvalidOperationException>(() =>
        {
            Render(builder =>
            {
                builder.OpenComponent<MenuRoot>(0);
                builder.AddAttribute(1, "DefaultOpen", true);
                builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
                {
                    innerBuilder.OpenComponent<MenuTrigger>(0);
                    innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                    innerBuilder.CloseComponent();

                    innerBuilder.OpenComponent<MenuPositioner>(2);
                    innerBuilder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<MenuPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            // MenuSubmenuTrigger without MenuSubmenuRoot
                            popupBuilder.OpenComponent<MenuSubmenuTrigger>(0);
                            popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Invalid Submenu")));
                            popupBuilder.CloseComponent();
                        }));
                        posBuilder.CloseComponent();
                    }));
                    innerBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            });
        });

        return Task.CompletedTask;
    }
}
