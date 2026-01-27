namespace BlazorBaseUI.Tests.Accordion;

public class AccordionItemTests : BunitContext, IAccordionItemContract
{
    public AccordionItemTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupAccordionModules(JSInterop);
    }

    private RenderFragment CreateAccordionWithItem(
        string itemValue = "test-item",
        bool itemDisabled = false,
        bool rootDisabled = false,
        string[]? defaultValue = null,
        Orientation orientation = Orientation.Vertical,
        Func<AccordionItemState<string>, string>? classValue = null,
        Func<AccordionItemState<string>, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        string? asElement = null)
    {
        return builder =>
        {
            builder.OpenComponent<AccordionRoot<string>>(0);
            builder.AddAttribute(1, "DefaultValue", defaultValue ?? Array.Empty<string>());
            builder.AddAttribute(2, "Disabled", rootDisabled);
            builder.AddAttribute(3, "Orientation", orientation);
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<AccordionItem<string>>(0);
                innerBuilder.AddAttribute(1, "Value", itemValue);
                innerBuilder.AddAttribute(2, "Disabled", itemDisabled);
                if (classValue is not null)
                    innerBuilder.AddAttribute(3, "ClassValue", classValue);
                if (styleValue is not null)
                    innerBuilder.AddAttribute(4, "StyleValue", styleValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(5, "AdditionalAttributes", additionalAttributes);
                if (asElement is not null)
                    innerBuilder.AddAttribute(6, "As", asElement);
                innerBuilder.AddAttribute(7, "ChildContent", CreateItemContent());
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateItemContent()
    {
        return builder =>
        {
            builder.OpenComponent<AccordionHeader>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(b =>
            {
                b.OpenComponent<AccordionTrigger>(0);
                b.AddAttribute(1, "ChildContent", (RenderFragment)(tb => tb.AddContent(0, "Trigger")));
                b.CloseComponent();
            }));
            builder.CloseComponent();

            builder.OpenComponent<AccordionPanel>(2);
            builder.AddAttribute(3, "KeepMounted", true);
            builder.AddAttribute(4, "ChildContent", (RenderFragment)(pb => pb.AddContent(0, "Panel Content")));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateAccordionWithItem());

        var items = cut.FindAll("div[data-index]");
        items.Count.ShouldBeGreaterThan(0);

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomAs()
    {
        var cut = Render(CreateAccordionWithItem(asElement: "section"));

        var item = cut.Find("section[data-index]");
        item.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateAccordionWithItem(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "accordion-item" },
                { "aria-label", "Item" }
            }
        ));

        cut.Markup.ShouldContain("data-testid=\"accordion-item\"");
        cut.Markup.ShouldContain("aria-label=\"Item\"");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateAccordionWithItem(
            classValue: _ => "custom-item-class"
        ));

        cut.Markup.ShouldContain("custom-item-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateAccordionWithItem(
            styleValue: _ => "margin: 10px"
        ));

        cut.Markup.ShouldContain("margin: 10px");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateAccordionWithItem(defaultValue: ["test-item"]));

        var item = cut.Find("div[data-index][data-open]");
        item.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreateAccordionWithItem());

        var item = cut.Find("div[data-index][data-closed]");
        item.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateAccordionWithItem(itemDisabled: true));

        var item = cut.Find("div[data-index][data-disabled]");
        item.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenRootDisabled()
    {
        var cut = Render(CreateAccordionWithItem(rootDisabled: true));

        var item = cut.Find("div[data-index][data-disabled]");
        item.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataIndexAttribute()
    {
        var cut = Render(CreateAccordionWithItem());

        var item = cut.Find("div[data-index]");
        item.ShouldNotBeNull();
        item.HasAttribute("data-index").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOrientationAttribute()
    {
        var cut = Render(CreateAccordionWithItem(orientation: Orientation.Horizontal));

        var item = cut.Find("div[data-index]");
        item.GetAttribute("data-orientation").ShouldBe("horizontal");

        return Task.CompletedTask;
    }
}
