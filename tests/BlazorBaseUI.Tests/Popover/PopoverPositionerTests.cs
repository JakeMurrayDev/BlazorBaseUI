using BlazorBaseUI.Popover;
using BlazorBaseUI.Tests.Contracts.Popover;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Popover;

public class PopoverPositionerTests : BunitContext, IPopoverPositionerContract
{
    public PopoverPositionerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPopoverModule(JSInterop);
    }

    private RenderFragment CreatePositionerInPopover(
        bool defaultOpen = true,
        RenderFragment<RenderProps<PopoverPositionerState>>? render = null,
        BlazorBaseUI.Popover.Side side = BlazorBaseUI.Popover.Side.Bottom,
        BlazorBaseUI.Popover.Align align = BlazorBaseUI.Popover.Align.Center,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PopoverPositionerState, string>? classValue = null,
        Func<PopoverPositionerState, string>? styleValue = null)
    {
        return builder =>
        {
            builder.OpenComponent<PopoverRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", defaultOpen);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<PopoverTrigger>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();

                innerBuilder.OpenComponent<PopoverPortal>(10);
                innerBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(portalBuilder =>
                {
                    portalBuilder.OpenComponent<PopoverPositioner>(0);
                    var attrIndex = 1;

                    if (render is not null)
                        portalBuilder.AddAttribute(attrIndex++, "Render", render);
                    portalBuilder.AddAttribute(attrIndex++, "Side", side);
                    portalBuilder.AddAttribute(attrIndex++, "Align", align);
                    if (classValue is not null)
                        portalBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                    if (styleValue is not null)
                        portalBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                    if (additionalAttributes is not null)
                        portalBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);

                    portalBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(posBuilder =>
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
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreatePositionerInPopover());

        var positioner = cut.Find("div[role='presentation']");
        positioner.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomRender()
    {
        RenderFragment<RenderProps<PopoverPositionerState>> render = props => builder =>
        {
            builder.OpenElement(0, "section");
            builder.AddMultipleAttributes(1, props.Attributes);
            if (props.ElementReferenceCallback is not null)
                builder.AddElementReferenceCapture(2, props.ElementReferenceCallback);
            builder.AddContent(3, props.ChildContent);
            builder.CloseElement();
        };

        var cut = Render(CreatePositionerInPopover(render: render));

        var positioner = cut.Find("section[role='presentation']");
        positioner.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreatePositionerInPopover(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "positioner" },
                { "aria-label", "Positioner" }
            }
        ));

        var positioner = cut.Find("[data-testid='positioner']");
        positioner.GetAttribute("aria-label").ShouldBe("Positioner");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataSideAttribute()
    {
        var cut = Render(CreatePositionerInPopover(side: BlazorBaseUI.Popover.Side.Top));

        var positioner = cut.Find("[role='presentation']");
        positioner.GetAttribute("data-side").ShouldBe("top");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataAlignAttribute()
    {
        var cut = Render(CreatePositionerInPopover(align: BlazorBaseUI.Popover.Align.Start));

        var positioner = cut.Find("[role='presentation']");
        positioner.GetAttribute("data-align").ShouldBe("start");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreatePositionerInPopover(
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var positioner = cut.Find("[role='presentation']");
        positioner.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreatePositionerInPopover(
            styleValue: _ => "z-index: 100"
        ));

        var positioner = cut.Find("[role='presentation']");
        positioner.GetAttribute("style")!.ShouldContain("z-index: 100");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<PopoverPositioner>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
