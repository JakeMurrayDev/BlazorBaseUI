using BlazorBaseUI.Popover;
using BlazorBaseUI.Tests.Contracts.Popover;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Popover;

public class PopoverPopupTests : BunitContext, IPopoverPopupContract
{
    public PopoverPopupTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPopoverModule(JSInterop);
    }

    private RenderFragment CreatePopupInPopover(
        bool defaultOpen = true,
        string? asElement = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        Func<PopoverPopupState, string>? classValue = null,
        Func<PopoverPopupState, string>? styleValue = null,
        RenderFragment? childContent = null,
        bool includeTitle = false,
        bool includeDescription = false)
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
                    portalBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<PopoverPopup>(0);
                        var attrIndex = 1;

                        if (asElement is not null)
                            posBuilder.AddAttribute(attrIndex++, "As", asElement);
                        if (classValue is not null)
                            posBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                        if (styleValue is not null)
                            posBuilder.AddAttribute(attrIndex++, "StyleValue", styleValue);
                        if (additionalAttributes is not null)
                            posBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);

                        posBuilder.AddAttribute(attrIndex++, "ChildContent", childContent ?? ((RenderFragment)(popupBuilder =>
                        {
                            if (includeTitle)
                            {
                                popupBuilder.OpenComponent<PopoverTitle>(0);
                                popupBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Title")));
                                popupBuilder.CloseComponent();
                            }

                            if (includeDescription)
                            {
                                popupBuilder.OpenComponent<PopoverDescription>(10);
                                popupBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Description")));
                                popupBuilder.CloseComponent();
                            }

                            popupBuilder.AddContent(20, "Content");
                        })));

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
        var cut = Render(CreatePopupInPopover());

        var popup = cut.Find("[role='dialog']");
        popup.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomAs()
    {
        var cut = Render(CreatePopupInPopover(asElement: "section"));

        var popup = cut.Find("section[role='dialog']");
        popup.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreatePopupInPopover(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "popup" },
                { "aria-label", "Popup" }
            }
        ));

        var popup = cut.Find("[data-testid='popup']");
        popup.GetAttribute("aria-label").ShouldBe("Popup");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersChildren()
    {
        var cut = Render(CreatePopupInPopover());

        var popup = cut.Find("[role='dialog']");
        popup.TextContent.ShouldContain("Content");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRoleDialog()
    {
        var cut = Render(CreatePopupInPopover());

        var popup = cut.Find("[role='dialog']");
        popup.GetAttribute("role").ShouldBe("dialog");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaModal()
    {
        var cut = Render(CreatePopupInPopover());

        var popup = cut.Find("[role='dialog']");
        popup.HasAttribute("tabindex").ShouldBeTrue();
        popup.GetAttribute("tabindex").ShouldBe("-1");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreatePopupInPopover(defaultOpen: true));

        var popup = cut.Find("[role='dialog']");
        popup.HasAttribute("data-open").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaLabelledByWhenTitlePresent()
    {
        var cut = Render(CreatePopupInPopover(includeTitle: true));

        var popup = cut.Find("[role='dialog']");
        popup.HasAttribute("aria-labelledby").ShouldBeTrue();

        var title = cut.Find("h2");
        popup.GetAttribute("aria-labelledby").ShouldBe(title.GetAttribute("id"));

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaDescribedByWhenDescriptionPresent()
    {
        var cut = Render(CreatePopupInPopover(includeDescription: true));

        var popup = cut.Find("[role='dialog']");
        popup.HasAttribute("aria-describedby").ShouldBeTrue();

        var description = cut.Find("p");
        popup.GetAttribute("aria-describedby").ShouldBe(description.GetAttribute("id"));

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValueWithState()
    {
        var cut = Render(CreatePopupInPopover(
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var popup = cut.Find("[role='dialog']");
        popup.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValueWithState()
    {
        var cut = Render(CreatePopupInPopover(
            styleValue: _ => "background: white"
        ));

        var popup = cut.Find("[role='dialog']");
        popup.GetAttribute("style")!.ShouldContain("background: white");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<PopoverPopup>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Content"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
