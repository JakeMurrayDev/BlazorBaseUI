namespace BlazorBaseUI.Tests.NavigationMenu;

public class NavigationMenuLinkTests : BunitContext, INavigationMenuLinkContract
{
    public NavigationMenuLinkTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
    }

    private RenderFragment CreateLinkInRoot(
        bool active = false,
        string? href = null,
        Func<NavigationMenuLinkState, string>? classValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuLink>(0);
                var attrIndex = 1;
                innerBuilder.AddAttribute(attrIndex++, "Active", active);
                if (href is not null)
                    innerBuilder.AddAttribute(attrIndex++, "Href", href);
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                innerBuilder.AddAttribute(attrIndex++, "ChildContent", (RenderFragment)(b => b.AddContent(0, "Link")));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAnchorByDefault()
    {
        var cut = Render(CreateLinkInRoot());

        var link = cut.Find("a");
        link.TagName.ShouldBe("A");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateLinkInRoot(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "nav-link" } }
        ));

        var link = cut.Find("a");
        link.GetAttribute("data-testid").ShouldBe("nav-link");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaCurrentPageWhenActive()
    {
        var cut = Render(CreateLinkInRoot(active: true));

        var link = cut.Find("a");
        link.GetAttribute("aria-current").ShouldBe("page");

        return Task.CompletedTask;
    }

    [Fact]
    public Task NoAriaCurrentWhenInactive()
    {
        var cut = Render(CreateLinkInRoot(active: false));

        var link = cut.Find("a");
        link.HasAttribute("aria-current").ShouldBeFalse();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasHref()
    {
        var cut = Render(CreateLinkInRoot(href: "/about"));

        var link = cut.Find("a");
        link.GetAttribute("href").ShouldBe("/about");

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateLinkInRoot(
            active: true,
            classValue: state => state.Active ? "active-link" : "link"
        ));

        var link = cut.Find("a");
        link.GetAttribute("class")!.ShouldContain("active-link");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuLink>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Link"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
