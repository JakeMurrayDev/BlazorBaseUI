namespace BlazorBaseUI.Tests.NavigationMenu;

public class NavigationMenuBackdropTests : BunitContext, INavigationMenuBackdropContract
{
    public NavigationMenuBackdropTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
    }

    private RenderFragment CreateBackdropInRoot(
        Func<NavigationMenuBackdropState, string>? classValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuBackdrop>(0);
                var attrIndex = 1;
                if (classValue is not null)
                    innerBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                if (additionalAttributes is not null)
                    innerBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersDivByDefault()
    {
        var cut = Render(CreateBackdropInRoot());

        var div = cut.Find("div[role='presentation']");
        div.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateBackdropInRoot(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "backdrop" } }
        ));

        var div = cut.Find("div[data-testid='backdrop']");
        div.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasRolePresentation()
    {
        var cut = Render(CreateBackdropInRoot());

        var div = cut.Find("div[role='presentation']");
        div.GetAttribute("role").ShouldBe("presentation");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataClosedWhenClosed()
    {
        var cut = Render(CreateBackdropInRoot());

        var div = cut.Find("div[role='presentation']");
        div.HasAttribute("data-closed").ShouldBeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateBackdropInRoot(
            classValue: _ => "backdrop-class"
        ));

        var div = cut.Find("div[role='presentation']");
        div.GetAttribute("class")!.ShouldContain("backdrop-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuBackdrop>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Backdrop"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
