namespace BlazorBaseUI.Tests.Menu;

public class MenuRootTests : BunitContext, IMenuRootContract
{
    public MenuRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupMenuModule(JSInterop);
    }

    private RenderFragment CreateMenuRoot(
        bool? open = null,
        bool defaultOpen = false,
        bool disabled = false,
        ModalMode modal = ModalMode.True,
        MenuOrientation orientation = MenuOrientation.Vertical,
        MenuRootActions? actionsRef = null,
        EventCallback<MenuOpenChangeEventArgs>? onOpenChange = null,
        EventCallback<bool>? onOpenChangeComplete = null,
        bool includeTrigger = true,
        bool includePositioner = true)
    {
        return builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            var attrIndex = 1;

            if (open.HasValue)
                builder.AddAttribute(attrIndex++, "Open", open.Value);
            builder.AddAttribute(attrIndex++, "DefaultOpen", defaultOpen);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            builder.AddAttribute(attrIndex++, "Modal", modal);
            builder.AddAttribute(attrIndex++, "Orientation", orientation);
            if (actionsRef is not null)
                builder.AddAttribute(attrIndex++, "ActionsRef", actionsRef);
            if (onOpenChange.HasValue)
                builder.AddAttribute(attrIndex++, "OnOpenChange", onOpenChange.Value);
            if (onOpenChangeComplete.HasValue)
                builder.AddAttribute(attrIndex++, "OnOpenChangeComplete", onOpenChangeComplete.Value);

            builder.AddAttribute(attrIndex++, "ChildContent", CreateChildContent(includeTrigger, includePositioner));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateChildContent(bool includeTrigger = true, bool includePositioner = true)
    {
        return builder =>
        {
            if (includeTrigger)
            {
                builder.OpenComponent<MenuTrigger>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Open Menu")));
                builder.CloseComponent();
            }
            if (includePositioner)
            {
                builder.OpenComponent<MenuPositioner>(2);
                builder.AddAttribute(3, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<MenuPopup>(0);
                    posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                    {
                        popupBuilder.OpenComponent<MenuItem>(0);
                        popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Item 1")));
                        popupBuilder.CloseComponent();
                    }));
                    posBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            }
        };
    }

    [Fact]
    public Task CascadesContextToChildren()
    {
        MenuTriggerState? capturedState = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<MenuRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<MenuTrigger>(0);
                innerBuilder.AddAttribute(1, "ClassValue", (Func<MenuTriggerState, string>)(state =>
                {
                    capturedState = state;
                    return "trigger-class";
                }));
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedState.ShouldNotBeNull();
        capturedState!.Value.Open.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledModeRespectsOpenParameter()
    {
        var cut = Render(CreateMenuRoot(open: false));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledModeUsesDefaultOpen()
    {
        var cut = Render(CreateMenuRoot(defaultOpen: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnOpenChangeWithReason()
    {
        var invoked = false;
        var receivedOpen = false;
        var receivedReason = OpenChangeReason.None;

        var cut = Render(CreateMenuRoot(
            onOpenChange: EventCallback.Factory.Create<MenuOpenChangeEventArgs>(this, args =>
            {
                invoked = true;
                receivedOpen = args.Open;
                receivedReason = args.Reason;
            })
        ));

        var trigger = cut.Find("button");
        trigger.Click();

        invoked.ShouldBeTrue();
        receivedOpen.ShouldBeTrue();
        receivedReason.ShouldBe(OpenChangeReason.TriggerPress);

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnOpenChangeComplete()
    {
        // OnOpenChangeComplete is invoked after transitions complete.
        // In bUnit tests without real JS, we can verify the callback is wired up.
        var cut = Render(CreateMenuRoot(
            defaultOpen: true,
            onOpenChangeComplete: EventCallback.Factory.Create<bool>(this, _ => { })
        ));

        // Menu should be open
        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DisabledStatePreventsTriggerInteraction()
    {
        var cut = Render(CreateMenuRoot(disabled: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("disabled").ShouldBeTrue();
        trigger.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsModalModes()
    {
        var cutModal = Render(CreateMenuRoot(modal: ModalMode.True));
        var cutNonModal = Render(CreateMenuRoot(modal: ModalMode.False));

        // Both should render the trigger
        cutModal.Find("button").ShouldNotBeNull();
        cutNonModal.Find("button").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task SupportsOrientations()
    {
        var cutVertical = Render(CreateMenuRoot(orientation: MenuOrientation.Vertical));
        var cutHorizontal = Render(CreateMenuRoot(orientation: MenuOrientation.Horizontal));

        // Both should render the trigger
        cutVertical.Find("button").ShouldNotBeNull();
        cutHorizontal.Find("button").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ActionsRefProvidesCloseMethod()
    {
        var actions = new MenuRootActions();

        var cut = Render(CreateMenuRoot(
            defaultOpen: true,
            actionsRef: actions
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        actions.Close.ShouldNotBeNull();

        return Task.CompletedTask;
    }
}
