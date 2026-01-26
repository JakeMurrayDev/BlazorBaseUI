namespace BlazorBaseUI.Tests.Collapsible;

public class CollapsibleRootTest : BunitContext
{
    public CollapsibleRootTest()
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
    public void DisabledStatus_HasDataDisabledAttribute()
    {
        var cut = Render<CollapsibleRoot>(parameters => parameters
            .Add(p => p.Disabled, true)
            .Add(p => p.ChildContent, CreateCollapsibleChildContent())
        );

        var trigger = cut.Find("button");
        trigger.HasAttribute("data-disabled").ShouldBeTrue();
    }

    [Fact]
    public void ControlledMode_OpenStateControlled()
    {
        var cut = Render<CollapsibleRoot>(parameters => parameters
            .Add(p => p.Open, false)
            .Add(p => p.ChildContent, CreateCollapsibleChildContent())
        );

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Fact]
    public void UncontrolledMode_DefaultOpenFalse_HasAriaExpandedFalse()
    {
        var cut = Render<CollapsibleRoot>(parameters => parameters
            .Add(p => p.DefaultOpen, false)
            .Add(p => p.ChildContent, CreateCollapsibleChildContent())
        );

        var trigger = cut.Find("button");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Fact]
    public void ARIAAttributes_SetAriaAttributes()
    {
        var cut = Render<CollapsibleRoot>(parameters => parameters
            .Add(p => p.DefaultOpen, true)
            .Add(p => p.ChildContent, CreateCollapsibleChildContent())
        );

        var trigger = cut.Find("button");

        trigger.GetAttribute("aria-expanded").ShouldBe("true");
        trigger.HasAttribute("aria-controls").ShouldBeTrue();
    }
}
