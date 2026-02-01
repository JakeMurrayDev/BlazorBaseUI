using BlazorBaseUI.Popover;
using BlazorBaseUI.Tests.Contracts.Popover;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Popover;

public class PopoverDescriptionTests : BunitContext, IPopoverDescriptionContract
{
    public PopoverDescriptionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPopoverModule(JSInterop);
    }

    private RenderFragment CreateDescriptionInPopover(
        bool defaultOpen = true,
        string? asElement = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
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
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(popupBuilder =>
                        {
                            popupBuilder.OpenComponent<PopoverDescription>(0);
                            var attrIndex = 1;

                            if (asElement is not null)
                                popupBuilder.AddAttribute(attrIndex++, "As", asElement);
                            if (additionalAttributes is not null)
                                popupBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);
                            popupBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Description text")));

                            popupBuilder.CloseComponent();
                        }));
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
    public Task RendersAsParagraphByDefault()
    {
        var cut = Render(CreateDescriptionInPopover());

        var description = cut.Find("p");
        description.TagName.ShouldBe("P");
        description.TextContent.ShouldBe("Description text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomAs()
    {
        var cut = Render(CreateDescriptionInPopover(asElement: "span"));

        var description = cut.Find("span");
        description.TextContent.ShouldBe("Description text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateDescriptionInPopover(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "description" },
                { "aria-label", "Description" }
            }
        ));

        var description = cut.Find("[data-testid='description']");
        description.GetAttribute("aria-label").ShouldBe("Description");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaDescribedByOnPopup()
    {
        var cut = Render(CreateDescriptionInPopover());

        var description = cut.Find("p");
        var descriptionId = description.GetAttribute("id");

        var popup = cut.Find("[role='dialog']");
        popup.GetAttribute("aria-describedby").ShouldBe(descriptionId);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<PopoverDescription>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Description"))
        );

        cut.Find("p").ShouldNotBeNull();

        return Task.CompletedTask;
    }
}
