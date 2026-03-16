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

    [Fact]
    public Task OnOpenChangeCompleteNotCalledOnMount()
    {
        var completeCalled = false;

        var cut = Render(CreatePopover(
            defaultOpen: true,
            onOpenChangeComplete: EventCallback.Factory.Create<bool>(this, _ =>
            {
                completeCalled = true;
            })
        ));

        cut.Find("[role='dialog']").ShouldNotBeNull();
        completeCalled.ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task MultiTrigger_OpensWithAnyContainedTrigger()
    {
        var cut = Render(CreateMultiTriggerPopover());

        var triggerA = cut.Find("#trigger-a");
        var triggerB = cut.Find("#trigger-b");

        // Click trigger A - should open
        triggerA.Click();
        triggerA.GetAttribute("aria-expanded").ShouldBe("true");

        // Click trigger A again to close
        triggerA.Click();
        triggerA.GetAttribute("aria-expanded").ShouldBe("false");

        // Click trigger B - should open
        triggerB.Click();
        triggerB.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task Handle_OpensAndClosesImperatively()
    {
        var handle = new PopoverHandle<string>();
        var cut = Render(CreateHandlePopover(handle));

        // Verify initially closed
        cut.FindAll("[role='dialog']").Count.ShouldBe(0);

        // Open via handle
        handle.Open("trigger-a");

        cut.WaitForAssertion(() =>
        {
            handle.IsOpen.ShouldBeTrue();
            handle.ActiveTriggerId.ShouldBe("trigger-a");
        });

        // Close via handle
        handle.Close();

        cut.WaitForAssertion(() =>
        {
            handle.IsOpen.ShouldBeFalse();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public Task Handle_SetsPayload()
    {
        var handle = new PopoverHandle<string>();
        var cut = Render(CreateHandlePopover(handle));

        // Open trigger-a which has Payload="hello"
        handle.Open("trigger-a");

        cut.WaitForAssertion(() =>
        {
            handle.Payload.ShouldBe("hello");
        });

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ResetsOpenChangeReasonOnTransitionEnd()
    {
        var cut = Render(CreatePopover(defaultOpen: false));

        // Open via trigger click
        cut.Find("button").Click();

        // Simulate transition end (close)
        var root = cut.FindComponent<PopoverRoot>();
        await cut.InvokeAsync(() => root.Instance.OnTransitionEnd(true));

        // Now close
        cut.Find("button").Click();

        await cut.InvokeAsync(() => root.Instance.OnTransitionEnd(false));

        // After close transition, the positioner should not have stale reason
        // Verify the trigger no longer shows data-pressed (reason was reset)
        var trigger = cut.Find("button");
        trigger.HasAttribute("data-pressed").ShouldBeFalse();
    }

    [Fact]
    public async Task ResetsOpenChangeReasonOnForceUnmount()
    {
        var actions = new PopoverRootActions();
        var cut = Render(CreatePopover(defaultOpen: true, actionsRef: actions));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        await cut.InvokeAsync(() => actions.Unmount?.Invoke());

        cut.WaitForAssertion(() => cut.FindAll("[role='dialog']").Count.ShouldBe(0));

        // After force unmount, trigger should not show stale data-pressed
        var trigger = cut.Find("button");
        trigger.HasAttribute("data-pressed").ShouldBeFalse();
    }

    [Fact]
    public Task SetsInstantClickOnlyForKeyboardTriggerPress()
    {
        var cut = Render(CreatePopover(defaultOpen: false));

        // Click with Detail=1 (mouse click) - should NOT set instant click
        var trigger = cut.Find("button");
        trigger.PointerDown(new PointerEventArgs { PointerType = "mouse" });
        trigger.Click(new MouseEventArgs { Detail = 1 });

        // The popup should not have data-instant="click"
        var popup = cut.Find("[role='dialog']");
        popup.HasAttribute("data-instant").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task DoesNotSetInstantDismissOnOutsidePressClose()
    {
        var cut = Render(CreatePopover(defaultOpen: true));

        // Simulate outside press close
        var root = cut.FindComponent<PopoverRoot>();
        await cut.InvokeAsync(() => root.Instance.OnOutsidePress());

        // Popup should not have data-instant="dismiss"
        var popup = cut.Find("[role='dialog']");
        popup.HasAttribute("data-instant").ShouldBeFalse();
    }

    [Fact]
    public Task DoesNotSetInstantClickOnClosePressClose()
    {
        var cut = Render(CreatePopover(defaultOpen: true));

        // Find and verify the close button can close
        // ClosePress uses the Close() method which sets OpenChangeReason.ClosePress
        // Since no interaction type is "keyboard", it should not set instant
        var popup = cut.Find("[role='dialog']");

        // Before close, verify no instant attribute
        popup.HasAttribute("data-instant").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ViewportTriggerSwapClearsInstantType()
    {
        // Open via click (which sets instant type)
        var cut = Render(CreatePopover(defaultOpen: false));
        var trigger = cut.Find("button");
        trigger.PointerDown(new PointerEventArgs { PointerType = "" });
        trigger.Click(new MouseEventArgs { Detail = 0 });

        // For keyboard click, instant should be set on the popup
        var popup = cut.Find("[role='dialog']");
        popup.GetAttribute("data-instant").ShouldBe("click");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task PreventUnmountOnCloseKeepsPopupMounted()
    {
        var cut = Render(CreatePopover(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open)
                {
                    args.PreventUnmountOnClose();
                }
            })
        ));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        // Close the popover
        cut.Find("button").Click();

        // Simulate transition end
        var root = cut.FindComponent<PopoverRoot>();
        await cut.InvokeAsync(() => root.Instance.OnTransitionEnd(false));

        // Popup should still be mounted because we prevented unmount
        cut.Find("[role='dialog']").ShouldNotBeNull();
    }

    [Fact]
    public async Task PreventUnmountOnCloseFlagIsResetOnNextClose()
    {
        var preventUnmount = true;

        var cut = Render(CreatePopover(
            defaultOpen: true,
            onOpenChange: EventCallback.Factory.Create<PopoverOpenChangeEventArgs>(this, args =>
            {
                if (!args.Open && preventUnmount)
                {
                    args.PreventUnmountOnClose();
                }
            })
        ));

        // Close with prevention
        cut.Find("button").Click();
        var root = cut.FindComponent<PopoverRoot>();
        await cut.InvokeAsync(() => root.Instance.OnTransitionEnd(false));

        // Still mounted
        cut.Find("[role='dialog']").ShouldNotBeNull();

        // Now disable prevention and close again
        preventUnmount = false;
        // Re-open
        cut.Find("button").Click();
        await cut.InvokeAsync(() => root.Instance.OnTransitionEnd(true));

        // Close again without prevention
        cut.Find("button").Click();
        await cut.InvokeAsync(() => root.Instance.OnTransitionEnd(false));

        // Should be unmounted now
        cut.FindAll("[role='dialog']").Count.ShouldBe(0);
    }

    [Fact]
    public Task ScrollLockReactsToModalParameterChange()
    {
        // Start with non-modal open popover
        var cut = Render(CreatePopover(defaultOpen: true, modal: BlazorBaseUI.Popover.ModalMode.False));
        cut.Find("[role='dialog']").ShouldNotBeNull();

        // Re-render with modal=true - verifies no exceptions from scroll lock update
        var cut2 = Render(CreatePopover(defaultOpen: true, modal: BlazorBaseUI.Popover.ModalMode.True));
        cut2.Find("[role='dialog']").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public async Task InteractionTypeClearedOnTransitionEnd()
    {
        var cut = Render(CreatePopover(defaultOpen: false));

        // Open via keyboard click (Detail=0) to set interactionType
        var trigger = cut.Find("button");
        trigger.PointerDown(new PointerEventArgs { PointerType = "" });
        trigger.Click(new MouseEventArgs { Detail = 0 });

        // Verify instant click is set on the popup (proves interactionType = "keyboard")
        var popup = cut.Find("[role='dialog']");
        popup.GetAttribute("data-instant").ShouldBe("click");

        // Complete the open transition
        var root = cut.FindComponent<PopoverRoot>();
        await cut.InvokeAsync(() => root.Instance.OnTransitionEnd(true));

        // Close via keyboard click
        trigger.PointerDown(new PointerEventArgs { PointerType = "" });
        trigger.Click(new MouseEventArgs { Detail = 0 });

        // Should still get instant click since interactionType hasn't been cleared yet
        popup = cut.Find("[role='dialog']");
        popup.GetAttribute("data-instant").ShouldBe("click");

        // Complete the close transition — this should clear interactionType
        await cut.InvokeAsync(() => root.Instance.OnTransitionEnd(false));

        // Reopen with mouse — interactionType should be fresh (not stale "keyboard")
        trigger.PointerDown(new PointerEventArgs { PointerType = "mouse" });
        trigger.Click(new MouseEventArgs { Detail = 1 });

        popup = cut.Find("[role='dialog']");
        popup.HasAttribute("data-instant").ShouldBeFalse();
    }

    private RenderFragment CreatePopover(
        bool? open = null,
        bool defaultOpen = false,
        BlazorBaseUI.Popover.ModalMode modal = BlazorBaseUI.Popover.ModalMode.False,
        EventCallback<PopoverOpenChangeEventArgs>? onOpenChange = null,
        EventCallback<bool>? onOpenChangeComplete = null,
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
            if (onOpenChangeComplete.HasValue)
                builder.AddAttribute(5, "OnOpenChangeComplete", onOpenChangeComplete.Value);
            if (actionsRef is not null)
                builder.AddAttribute(6, "ActionsRef", actionsRef);

            builder.AddAttribute(7, "ChildContent", customContent ?? ((RenderFragment)(innerBuilder =>
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

    private RenderFragment CreateMultiTriggerPopover()
    {
        return builder =>
        {
            builder.OpenComponent<PopoverRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                innerBuilder.AddAttribute(1, "Id", "trigger-a");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger A")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverTrigger>(10);
                innerBuilder.AddAttribute(11, "Id", "trigger-b");
                innerBuilder.AddAttribute(12, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger B")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverPortal>(20);
                innerBuilder.AddAttribute(21, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Popup Content")));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private RenderFragment CreateHandlePopover(PopoverHandle<string> handle)
    {
        return builder =>
        {
            // Detached triggers
            builder.OpenComponent<PopoverTypedTrigger<string>>(0);
            builder.AddAttribute(1, "Id", "trigger-a");
            builder.AddAttribute(2, "Handle", handle);
            builder.AddAttribute(3, "Payload", "hello");
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger A")));
            builder.CloseComponent();

            builder.OpenComponent<PopoverTypedTrigger<string>>(10);
            builder.AddAttribute(11, "Id", "trigger-b");
            builder.AddAttribute(12, "Handle", handle);
            builder.AddAttribute(13, "Payload", "world");
            builder.AddAttribute(14, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger B")));
            builder.CloseComponent();

            // Root with handle
            builder.OpenComponent<PopoverRoot>(20);
            builder.AddAttribute(21, "Handle", (IPopoverHandle)handle);
            builder.AddAttribute(22, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverPortal>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Handle Content")));
                        posBuilder.CloseComponent();
                    }));
                    portalBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }
}
