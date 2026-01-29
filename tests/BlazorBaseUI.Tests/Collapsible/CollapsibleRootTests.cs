namespace BlazorBaseUI.Tests.Collapsible;

public class CollapsibleRootTests : BunitContext, ICollapsibleRootContract
{
    public CollapsibleRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupCollapsiblePanel(JSInterop);
    }

    private RenderFragment CreateCollapsibleRoot(
        bool? open = null,
        bool defaultOpen = false,
        bool disabled = false,
        Func<CollapsibleRootState, string>? classValue = null,
        Func<CollapsibleRootState, string>? styleValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null,
        string? asElement = null,
        EventCallback<CollapsibleOpenChangeEventArgs>? onOpenChange = null,
        bool includeTrigger = true,
        bool includePanel = true)
    {
        return builder =>
        {
            builder.OpenComponent<CollapsibleRoot>(0);
            var attrIndex = 1;

            if (open.HasValue)
                builder.AddAttribute(attrIndex++, "Open", open.Value);
            builder.AddAttribute(attrIndex++, "DefaultOpen", defaultOpen);
            if (disabled)
                builder.AddAttribute(attrIndex++, "Disabled", true);
            if (classValue is not null)
                builder.AddAttribute(attrIndex++, "ClassValue", classValue);
            if (styleValue is not null)
                builder.AddAttribute(attrIndex++, "StyleValue", styleValue);
            if (additionalAttributes is not null)
                builder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
            if (asElement is not null)
                builder.AddAttribute(attrIndex++, "As", asElement);
            if (onOpenChange.HasValue)
                builder.AddAttribute(attrIndex++, "OnOpenChange", onOpenChange.Value);

            builder.AddAttribute(attrIndex++, "ChildContent", CreateChildContent(includeTrigger, includePanel));
            builder.CloseComponent();
        };
    }

    private static RenderFragment CreateChildContent(bool includeTrigger = true, bool includePanel = true)
    {
        return builder =>
        {
            if (includeTrigger)
            {
                builder.OpenComponent<CollapsibleTrigger>(0);
                builder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                builder.CloseComponent();
            }
            if (includePanel)
            {
                builder.OpenComponent<CollapsiblePanel>(2);
                builder.AddAttribute(3, "KeepMounted", true);
                builder.AddAttribute(4, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Panel Content")));
                builder.CloseComponent();
            }
        };
    }

    [Fact]
    public Task RendersAsDivByDefault()
    {
        var cut = Render(CreateCollapsibleRoot());

        var div = cut.Find("div");
        div.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task RendersWithCustomAs()
    {
        var cut = Render(CreateCollapsibleRoot(asElement: "section"));

        var section = cut.Find("section");
        section.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateCollapsibleRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "collapsible-root" },
                { "aria-label", "Collapsible" }
            }
        ));

        cut.Markup.ShouldContain("data-testid=\"collapsible-root\"");
        cut.Markup.ShouldContain("aria-label=\"Collapsible\"");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateCollapsibleRoot(
            classValue: _ => "custom-class"
        ));

        cut.Markup.ShouldContain("custom-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesStyleValue()
    {
        var cut = Render(CreateCollapsibleRoot(
            styleValue: _ => "background: blue"
        ));

        cut.Markup.ShouldContain("background: blue");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CombinesClassFromBothSources()
    {
        var cut = Render(CreateCollapsibleRoot(
            classValue: _ => "dynamic-class",
            additionalAttributes: new Dictionary<string, object>
            {
                { "class", "static-class" }
            }
        ));

        cut.Markup.ShouldContain("static-class");
        cut.Markup.ShouldContain("dynamic-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task CascadesContextToChildren()
    {
        CollapsibleRootState? capturedState = null;

        var cut = Render(builder =>
        {
            builder.OpenComponent<CollapsibleRoot>(0);
            builder.AddAttribute(1, "DefaultOpen", true);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<CollapsibleTrigger>(0);
                innerBuilder.AddAttribute(1, "ClassValue", (Func<CollapsibleRootState, string>)(state =>
                {
                    capturedState = state;
                    return "trigger-class";
                }));
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Toggle")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        });

        capturedState.ShouldNotBeNull();
        capturedState!.Open.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task ControlledModeRespectsOpenParameter()
    {
        var cut = Render(CreateCollapsibleRoot(open: false));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task UncontrolledModeUsesDefaultOpen()
    {
        var cut = Render(CreateCollapsibleRoot(defaultOpen: true));

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task InvokesOnOpenChange()
    {
        var invoked = false;
        var receivedOpen = false;

        var cut = Render(CreateCollapsibleRoot(
            onOpenChange: EventCallback.Factory.Create<CollapsibleOpenChangeEventArgs>(this, args =>
            {
                invoked = true;
                receivedOpen = args.Open;
            })
        ));

        var trigger = cut.Find("button");
        trigger.Click();

        invoked.ShouldBeTrue();
        receivedOpen.ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataOpenWhenOpen()
    {
        var cut = Render(CreateCollapsibleRoot(defaultOpen: true));

        var root = cut.Find("div[data-open]");
        root.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreateCollapsibleRoot(defaultOpen: false));

        var root = cut.Find("div[data-closed]");
        root.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataDisabledWhenDisabled()
    {
        var cut = Render(CreateCollapsibleRoot(disabled: true));

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-disabled").ShouldBeTrue();

        return Task.CompletedTask;
    }
}
