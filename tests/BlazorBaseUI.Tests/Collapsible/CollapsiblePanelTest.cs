namespace BlazorBaseUI.Tests.Collapsible;

public class CollapsiblePanelTest : BunitContext
{
    public CollapsiblePanelTest()
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
    public void Render_Default_RendersAsDivElement()
    {
        var cut = Render<CollapsibleRoot>(parameters => parameters
            .Add(p => p.DefaultOpen, false)
            .Add(p => p.ChildContent, CreateCollapsibleChildContent())
        );

        var divs = cut.FindAll("div");
        divs.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void KeepMounted_PanelPresentWhenClosed()
    {
        var cut = Render<CollapsibleRoot>(parameters => parameters
            .Add(p => p.DefaultOpen, false)
            .Add(p => p.ChildContent, builder =>
            {
                builder.OpenComponent<CollapsibleTrigger>(0);
                builder.CloseComponent();
                builder.OpenComponent<CollapsiblePanel>(1);
                builder.AddAttribute(2, "KeepMounted", true);
                builder.CloseComponent();
            })
        );

        var trigger = cut.Find("button");
        var panel = cut.Find("div[data-closed]");

        trigger.GetAttribute("aria-expanded").ShouldBe("false");
        panel.HasAttribute("data-closed").ShouldBeTrue();
    }
}
