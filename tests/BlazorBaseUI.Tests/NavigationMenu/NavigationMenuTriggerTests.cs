namespace BlazorBaseUI.Tests.NavigationMenu;

public class NavigationMenuTriggerTests : BunitContext, INavigationMenuTriggerContract
{
    public NavigationMenuTriggerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
    }

    private RenderFragment CreateTriggerInRoot(
        string? defaultValue = null,
        bool triggerDisabled = false,
        bool includePopup = false,
        Func<NavigationMenuTriggerState, string>? classValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            var rootAttr = 1;
            if (defaultValue is not null)
                builder.AddAttribute(rootAttr++, "DefaultValue", defaultValue);
            builder.AddAttribute(rootAttr++, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuItem>(0);
                innerBuilder.AddAttribute(1, "Value", "item1");
                innerBuilder.AddAttribute(2, "ChildContent", (RenderFragment)(itemBuilder =>
                {
                    itemBuilder.OpenComponent<NavigationMenuTrigger>(0);
                    var attrIndex = 1;
                    if (triggerDisabled)
                        itemBuilder.AddAttribute(attrIndex++, "Disabled", true);
                    if (classValue is not null)
                        itemBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                    if (additionalAttributes is not null)
                        itemBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                    itemBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Trigger")));
                    itemBuilder.CloseComponent();

                    itemBuilder.OpenComponent<NavigationMenuContent>(10);
                    itemBuilder.AddAttribute(11, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Content")));
                    itemBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();

                if (includePopup)
                {
                    innerBuilder.OpenComponent<NavigationMenuPositioner>(4);
                    innerBuilder.AddAttribute(5, "ChildContent", (RenderFragment)(posBuilder =>
                    {
                        posBuilder.OpenComponent<NavigationMenuPopup>(0);
                        posBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Popup")));
                        posBuilder.CloseComponent();
                    }));
                    innerBuilder.CloseComponent();
                }
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersButtonByDefault()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.TagName.ShouldBe("BUTTON");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateTriggerInRoot(
            additionalAttributes: new Dictionary<string, object>
            {
                { "data-testid", "trigger" },
                { "aria-label", "Open nav" }
            }
        ));

        var button = cut.Find("button");
        button.GetAttribute("data-testid").ShouldBe("trigger");
        button.GetAttribute("aria-label").ShouldBe("Open nav");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedFalse()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaExpandedTrue()
    {
        var cut = Render(CreateTriggerInRoot(defaultValue: "item1"));

        var button = cut.Find("button");
        button.GetAttribute("aria-expanded").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NoAriaControlsWhenClosed()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.HasAttribute("aria-controls").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaControlsWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultValue: "item1", includePopup: true));

        var button = cut.Find("button");
        var ariaControls = button.GetAttribute("aria-controls");
        ariaControls.ShouldNotBeNullOrEmpty();

        // aria-controls should reference the popup element's auto-generated id
        var popup = cut.Find($"nav[id='{ariaControls}']");
        popup.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasTabIndex()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.GetAttribute("tabindex").ShouldBe("0");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NoDataPopupOpenWhenClosed()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.HasAttribute("data-popup-open").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataPopupOpenWhenOpen()
    {
        var cut = Render(CreateTriggerInRoot(defaultValue: "item1"));

        var button = cut.Find("button");
        button.HasAttribute("data-popup-open").ShouldBeTrue();
        button.HasAttribute("data-pressed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateTriggerInRoot(
            defaultValue: "item1",
            classValue: state => state.Open ? "open-class" : "closed-class"
        ));

        var button = cut.Find("button");
        button.GetAttribute("class")!.ShouldContain("open-class");

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ToggleOnClick()
    {
        var cut = Render(CreateTriggerInRoot());

        var button = cut.Find("button");
        button.GetAttribute("aria-expanded").ShouldBe("false");

        await button.TriggerEventAsync("onclick", new MouseEventArgs());
        cut.FindComponent<NavigationMenuTrigger>().Render();

        button = cut.Find("button");
        button.GetAttribute("aria-expanded").ShouldBe("true");
    }

    [Fact]
    public Task DoesNotToggleWhenDisabled()
    {
        var cut = Render(CreateTriggerInRoot(triggerDisabled: true));

        var button = cut.Find("button");
        button.GetAttribute("aria-expanded").ShouldBe("false");

        button.Click();

        button.GetAttribute("aria-expanded").ShouldBe("false");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuTrigger>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Trigger"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
