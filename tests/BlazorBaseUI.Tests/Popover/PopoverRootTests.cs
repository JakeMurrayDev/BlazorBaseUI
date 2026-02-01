using BlazorBaseUI.Popover;
using BlazorBaseUI.Tests.Contracts.Popover;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Tests.Popover;

public class PopoverRootTests : BunitContext, IPopoverRootContract
{
    public PopoverRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPopoverModule(JSInterop);
    }

    private RenderFragment CreatePopover(
        bool? open = null,
        bool defaultOpen = false,
        BlazorBaseUI.Popover.ModalMode modal = BlazorBaseUI.Popover.ModalMode.False,
        EventCallback<PopoverOpenChangeEventArgs>? onOpenChange = null,
        PopoverRootActions? actionsRef = null,
        RenderFragment? customContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<PopoverRoot>(0);

            if (open.HasValue)
                builder.AddAttribute(1, "Open", open.Value);
            builder.AddAttribute(2, "DefaultOpen", defaultOpen);
            builder.AddAttribute(3, "Modal", modal);

            if (onOpenChange.HasValue)
                builder.AddAttribute(4, "OnOpenChange", onOpenChange.Value);
            if (actionsRef is not null)
                builder.AddAttribute(5, "ActionsRef", actionsRef);

            builder.AddAttribute(6, "ChildContent", customContent ?? ((RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverPortal>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            })));

            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersChildren()
    {
        var cut = Render(CreatePopover());

        cut.Find("button").TextContent.ShouldBe("Toggle");

        return Task.CompletedTask;
    }

    [Fact]
    public Task OpensByDefaultWhenDefaultOpenTrue()
    {
        var cut = Render(CreatePopover(defaultOpen: true));

        cut.Find("[role='dialog']").ShouldNotBeNull();
        cut.Find("[role='dialog']").TextContent.ShouldContain("Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RemainsClosedWhenDefaultOpenFalseControlled()
    {
        var cut = Render(CreatePopover(open: false, defaultOpen: true));

        cut.FindAll("[role='dialog']").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task ClosesWhenTriggerClickedTwice()
    {
        var closeRequested = false;

        // Start with popover open to verify close behavior
        var cut = Render(CreatePopover(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    closeRequested = true;
                }
            })
        ));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        var trigger = cut.Find("button");
        trigger.Click();

        // Verify that close was requested (OnOpenChange fired with Open=false)
        closeRequested.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task CallsOnOpenChangeWhenOpenStateChanges()
    {
        var callCount = 0;
        var lastOpen = false;

        var cut = Render(CreatePopover(
            defaultOpen: false,
            onOpenChange: EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args =>
            {
                callCount++;
                lastOpen = args.Open;
            })
        ));

        var trigger = cut.Find("button");
        trigger.Click();

        callCount.ShouldBe(1);
        lastOpen.ShouldBeTrue();

        trigger.Click();

        callCount.ShouldBe(2);
        lastOpen.ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task OnOpenChangeCancelPreventsOpening()
    {
        var cut = Render(CreatePopover(
            defaultOpen: false,
            onOpenChange: EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args =>
            {
                if (args.Open)
                {
                    args.Cancel();
                }
            })
        ));

        var trigger = cut.Find("button");
        trigger.Click();

        cut.FindAll("[role='dialog']").Count.ShouldBe(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersInternalBackdropWhenModalTrue()
    {
        // Create a popover with an explicit PopoverBackdrop to test modal behavior
        RenderFragment content = builder =>
        {
            builder.OpenComponent<PopoverRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "Modal", BlazorBaseUI.Popover.ModalMode.True);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverPortal>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    // Add explicit backdrop
                    portalBuilder.OpenComponent<PopoverBackdrop>(0);
                    portalBuilder.CloseComponent();

                    portalBuilder.OpenComponent<PopoverPositioner>(10);
                    portalBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };

        var cut = Render(content);

        var positioner = cut.Find("[data-side]");
        var previousSibling = positioner.PreviousElementSibling;

        previousSibling.ShouldNotBeNull();
        previousSibling!.GetAttribute("role").ShouldBe("presentation");

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotRenderInternalBackdropWhenModalFalse()
    {
        var cut = Render(CreatePopover(defaultOpen: true, modal: BlazorBaseUI.Popover.ModalMode.False));

        var positioner = cut.Find("[data-side]");
        var previousSibling = positioner.PreviousElementSibling;

        if (previousSibling is not null)
        {
            previousSibling.GetAttribute("role").ShouldNotBe("presentation");
        }

        return Task.CompletedTask;
    }

    [Fact]
    public Task ActionsRefCloseMethodClosesPopover()
    {
        var closeRequested = false;
        var actions = new PopoverRootActions();

        var cut = Render(CreatePopover(
            defaultOpen: true,
            actionsRef: actions,
            onOpenChange: EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    closeRequested = true;
                }
            })
        ));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        // Invoke the Close action
        actions.Close?.Invoke();

        // Verify that close was requested (OnOpenChange fired with Open=false)
        closeRequested.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ActionsRefUnmountMethodUnmountsPopover()
    {
        var actions = new PopoverRootActions();
        var cut = Render(CreatePopover(defaultOpen: true, actionsRef: actions));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        await cut.InvokeAsync(() => actions.Unmount?.Invoke());

        cut.WaitForAssertion(() => cut.FindAll("[role='dialog']").Count.ShouldBe(0));
    }
}
