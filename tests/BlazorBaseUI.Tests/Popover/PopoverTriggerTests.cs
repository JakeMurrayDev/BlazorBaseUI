using BlazorBaseUI.Popover;
using BlazorBaseUI.Tests.Contracts.Popover;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Popover;

public class PopoverTriggerTests : BunitContext, IPopoverTriggerContract
{
    public PopoverTriggerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPopoverModule(JSInterop);
    }

    private RenderFragment CreateTriggerInRoot(
        bool defaultOpen = false,
        bool triggerDisabled = false,
        bool openOnHover = false,
        RenderFragment<RenderProps<PopoverTriggerState>>? render = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PopoverTriggerState, string>? classValue = null,
        Func<PopoverTriggerState, string>? styleValue = null,
        bool includePositioner = true)
    {
        return builder =>
        {
            builder.OpenComponent<PopoverRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                var attrIndex = 1;

                if (triggerDisabled)
                    innerBuilder.AddAttribute(attrIndex++, "Disabled", true);
                if (openOnHover)
                    innerBuilder.AddAttribute(attrIndex++, "OpenOnHover", true);
                if (render is not null)
                    innerBuilder.AddAttribute(attrIndex++, "Render", render);
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);
                innerBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                if (includePositioner)
                {
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
                }
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsButtonByDefault()
    {
        var cut = Render(CreateTriggerInRoot());

        var trigger = cut.Find("button");
        trigger.TagName.ShouldBe("BUTTON");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<PopoverTriggerState>> render = props => builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreateTriggerInRoot(render: render));

        var trigger = cut.Find("div[aria-expanded]");
        trigger.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateTriggerInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "trigger" },
                { "aria-label", "Open popover" }
            }
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("data-testid").ShouldBe("trigger");
        trigger.GetAttribute("aria-label").ShouldBe("Open popover");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHaspopupDialog()
    {
        var cut = Render(CreateTriggerInRoot());

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-haspopup").ShouldBe("dialog");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedFalseWhenClosed()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedTrueWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataPopupOpenWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-popup-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDisabledAttributeWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(triggerDisabled: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task DoesNotToggleWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false, triggerDisabled: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        trigger.Click();

        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task TogglesOnClick()
    {
        var cut = Render(CreateTriggerInRoot(defaultOpen: false));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        trigger.Click();

        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateTriggerInRoot(
            defaultOpen: true,
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateTriggerInRoot(
            styleValue: _ => "color: blue"
        ));

        var trigger = cut.Find("button");
        trigger.GetAttribute("style")!.ShouldContain("color: blue");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task HasFocusHandlersWhenOpenOnHover()
    {
        var cut = Render(CreateTriggerInRoot(openOnHover: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        await trigger.FocusAsync(new FocusEventArgs());

        trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<PopoverTrigger>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Toggle"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
