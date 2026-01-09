namespace BlazorBaseUI.Tests.Collapsible;

public class CollapsibleTriggerTest : BunitContext
{
    public CollapsibleTriggerTest()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static RenderFragment CreateCollapsibleChildContent()
    {
        return builder =>
        {
            builder.OpenComponent<CollapsibleTrigger>(0);
            builder.CloseComponent();
            builder.OpenComponent<CollapsiblePanel>(1);
            builder.CloseComponent();
        };
    }

    [Fact]
    public void Render_Default_RendersAsButtonElement()
    {
        var cut = Render<CollapsibleRoot>(parameters => parameters
            .Add(p => p.DefaultOpen, false)
            .Add(p => p.ChildContent, CreateCollapsibleChildContent())
        );

        var trigger = cut.Find("button");
        trigger.TagName.ShouldBe("BUTTON");
    }

    [Fact]
    public void DefaultOpenFalse_HasNoAriaControls()
    {
        var cut = Render<CollapsibleRoot>(parameters => parameters
            .Add(p => p.DefaultOpen, false)
            .Add(p => p.ChildContent, CreateCollapsibleChildContent())
        );

        var trigger = cut.Find("button");
        trigger.HasAttribute("aria-controls").ShouldBeFalse();
    }

    [Fact]
    public void Disabled_HasDisabledAttribute()
    {
        var cut = Render<CollapsibleRoot>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, CreateCollapsibleChildContent())
        );

        var trigger = cut.Find("button");
        trigger.HasAttribute("disabled").ShouldBeTrue();
    }
}
