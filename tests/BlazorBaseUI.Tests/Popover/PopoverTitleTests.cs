using BlazorBaseUI.Popover;
using BlazorBaseUI.Tests.Contracts.Popover;
using BlazorBaseUI.Tests.Infrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tests.Popover;

public class PopoverTitleTests : BunitContext, IPopoverTitleContract
{
    public PopoverTitleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupPopoverModule(JSInterop);
    }

    private RenderFragment CreateTitleInPopover(
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
                            popupBuilder.OpenComponent<PopoverTitle>(0);
                            var attrIndex = 1;

                            if (asElement is not null)
                                popupBuilder.AddAttribute(attrIndex++, "As", asElement);
                            if (additionalAttributes is not null)
                                popupBuilder.AddMultipleAttributes(attrIndex++, additionalAttributes);
                            popupBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Title text")));

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
    public Task RendersAsH2ByDefault()
    {
        var cut = Render(CreateTitleInPopover());

        var title = cut.Find("h2");
        title.TagName.ShouldBe("H2");
        title.TextContent.ShouldBe("Title text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomAs()
    {
        var cut = Render(CreateTitleInPopover(asElement: "h3"));

        var title = cut.Find("h3");
        title.TextContent.ShouldBe("Title text");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateTitleInPopover(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "title" },
                { "aria-label", "Title" }
            }
        ));

        var title = cut.Find("[data-testid='title']");
        title.GetAttribute("aria-label").ShouldBe("Title");

        return Task.CompletedTask;
    }

    [Fact]
    public Task SetsAriaLabelledByOnPopup()
    {
        var cut = Render(CreateTitleInPopover());

        var title = cut.Find("h2");
        var titleId = title.GetAttribute("id");

        var popup = cut.Find("[role='dialog']");
        popup.GetAttribute("aria-labelledby").ShouldBe(titleId);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<PopoverTitle>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Title"))
        );

        cut.Find("h2").ShouldNotBeNull();

        return Task.CompletedTask;
    }
}
