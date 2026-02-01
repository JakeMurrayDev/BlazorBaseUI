using BlazorBaseUI.Popover;
using BlazorBaseUI.Tests.Contracts.Popover;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Popover;

public class PopoverBackdropTests : BunitContext, IPopoverBackdropContract
{
    public PopoverBackdropTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPopoverModule(JSInterop);
    }

    private RenderFragment CreateBackdropInPopover(
        bool defaultOpen = true,
        string? asElement = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PopoverBackdropState, string>? classValue = null,
        Func<PopoverBackdropState, string>? styleValue = null)
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
                    portalBuilder.OpenComponent<PopoverBackdrop>(0);
                    var attrIndex = 1;

                    if (asElement is not null)
                        portalBuilder.AddAttribute(attrIndex++, "As", asElement);
                    if (classValue is not null)
                        portalBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                    if (styleValue is not null)
                        portalBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                    if (additionalAttributes is not null)
                        portalBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);

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
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateBackdropInPopover());

        var backdrop = cut.Find("div[role='presentation']");
        backdrop.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomAs()
    {
        var cut = Render(CreateBackdropInPopover(asElement: "span"));

        var backdrop = cut.Find("span[role='presentation']");
        backdrop.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateBackdropInPopover(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "backdrop" },
                { "aria-label", "Backdrop" }
            }
        ));

        var backdrop = cut.Find("[data-testid='backdrop']");
        backdrop.GetAttribute("aria-label").ShouldBe("Backdrop");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreateBackdropInPopover(
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var backdrop = cut.Find("div[role='presentation']");
        backdrop.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreateBackdropInPopover(
            styleValue: _ => "background: black"
        ));

        var backdrop = cut.Find("div[role='presentation']");
        backdrop.GetAttribute("style")!.ShouldContain("background: black");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateBackdropInPopover(defaultOpen: true));

        var backdrop = cut.Find("div[role='presentation']");
        backdrop.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<PopoverBackdrop>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
