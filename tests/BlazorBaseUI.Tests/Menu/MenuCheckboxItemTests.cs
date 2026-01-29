namespace BlazorBaseUI.Tests.Menu;

public class MenuCheckboxItemTests : BunitContext, IMenuCheckboxItemContract
{
    public MenuCheckboxItemTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateCheckboxItemInRoot(
        bool defaultOpen = true,
        bool? itemChecked = null,
        bool defaultChecked = false,
        bool itemDisabled = false,
        bool closeOnClick = false,
        EventCallback<MenuCheckboxItemChangeEventArgs>? onCheckedChange = null)
    {
        return builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
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
                        popupBuilder.OpenComponent<MenuCheckboxItem>(0);
                        var attrIndex = 1;

                        if (itemChecked.HasValue)
                            popupBuilder.AddAttribute(attrIndex++, "Checked", itemChecked.Value);
                        popupBuilder.AddAttribute(attrIndex++, "DefaultChecked", defaultChecked);
                        if (itemDisabled)
                            popupBuilder.AddAttribute(attrIndex++, "Disabled", true);
                        popupBuilder.AddAttribute(attrIndex++, "CloseOnClick", closeOnClick);
                        if (onCheckedChange.HasValue)
                            popupBuilder.AddAttribute(attrIndex++, "OnCheckedChange", onCheckedChange.Value);
                        popupBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Checkbox Item")));
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
    public Task HasRoleMenuitemcheckbox()
    {
        var cut = Render(CreateCheckboxItemInRoot());

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("role").ShouldBe("menuitemcheckbox");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaCheckedFalseWhenUnchecked()
    {
        var cut = Render(CreateCheckboxItemInRoot(defaultChecked: false));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaCheckedTrueWhenChecked()
    {
        var cut = Render(CreateCheckboxItemInRoot(defaultChecked: true));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataCheckedWhenChecked()
    {
        var cut = Render(CreateCheckboxItemInRoot(defaultChecked: true));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.HasAttribute("data-checked").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataUncheckedWhenUnchecked()
    {
        var cut = Render(CreateCheckboxItemInRoot(defaultChecked: false));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.HasAttribute("data-unchecked").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledModeRespectsCheckedParameter()
    {
        var cut = Render(CreateCheckboxItemInRoot(itemChecked: true));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("aria-checked").ShouldBe("true");

        // Clicking should not change the controlled value
        item.Click();

        // In controlled mode, the value stays the same until parent updates
        item.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledModeUsesDefaultChecked()
    {
        var cut = Render(CreateCheckboxItemInRoot(defaultChecked: true));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task TogglesOnClick()
    {
        var cut = Render(CreateCheckboxItemInRoot(defaultChecked: false));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("aria-checked").ShouldBe("false");

        item.Click();

        item.GetAttribute("aria-checked").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnCheckedChange()
    {
        var invoked = false;
        var receivedChecked = false;

        var cut = Render(CreateCheckboxItemInRoot(
            defaultChecked: false,
            onCheckedChange: EventCallback.Factory.Create<MenuCheckboxItemChangeEventArgs>(this, args =>
            {
                invoked = true;
                receivedChecked = args.Checked;
            })
        ));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.Click();

        invoked.ShouldBeTrue();
        receivedChecked.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsCancelInOnCheckedChange()
    {
        var cut = Render(CreateCheckboxItemInRoot(
            defaultChecked: false,
            onCheckedChange: EventCallback.Factory.Create<MenuCheckboxItemChangeEventArgs>(this, args =>
            {
                args.Cancel();
            })
        ));

        var item = cut.Find("[role='menuitemcheckbox']");
        item.GetAttribute("aria-checked").ShouldBe("false");

        item.Click();

        // State should not change because we canceled
        item.GetAttribute("aria-checked").ShouldBe("false");

        return Task.CompletedTask;
    }
}
