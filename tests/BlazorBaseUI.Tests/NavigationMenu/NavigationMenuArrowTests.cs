namespace BlazorBaseUI.Tests.NavigationMenu;

public class NavigationMenuArrowTests : BunitContext, INavigationMenuArrowContract
{
    public NavigationMenuArrowTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JsInteropSetup.SetupNavigationMenuModule(JSInterop);
    }

    private RenderFragment CreateArrowInRoot(
        Func<NavigationMenuArrowState, string>? classValue = null,
        IReadOnlyDictionary<string, object>? additionalAttributes = null)
    {
        return builder =>
        {
            builder.OpenComponent<NavigationMenuRoot>(0);
            builder.AddAttribute(1, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                innerBuilder.OpenComponent<NavigationMenuPositioner>(0);
                innerBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(posBuilder =>
                {
                    posBuilder.OpenComponent<NavigationMenuArrow>(0);
                    var attrIndex = 1;
                    if (classValue is not null)
                        posBuilder.AddAttribute(attrIndex++, "ClassValue", classValue);
                    if (additionalAttributes is not null)
                        posBuilder.AddAttribute(attrIndex++, "AdditionalAttributes", additionalAttributes);
                    posBuilder.CloseComponent();
                }));
                innerBuilder.CloseComponent();
            }));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersDivByDefault()
    {
        var cut = Render(CreateArrowInRoot());

        var div = cut.Find("div[aria-hidden='true']");
        div.TagName.ShouldBe("DIV");

        return Task.CompletedTask;
    }

    [Fact]
    public Task ForwardsAdditionalAttributes()
    {
        var cut = Render(CreateArrowInRoot(
            additionalAttributes: new Dictionary<string, object> { { "data-testid", "arrow" } }
        ));

        var div = cut.Find("div[data-testid='arrow']");
        div.ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasAriaHidden()
    {
        var cut = Render(CreateArrowInRoot());

        var div = cut.Find("div[aria-hidden='true']");
        div.GetAttribute("aria-hidden").ShouldBe("true");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataSide()
    {
        var cut = Render(CreateArrowInRoot());

        var div = cut.Find("div[aria-hidden='true']");
        div.GetAttribute("data-side").ShouldBe("bottom");

        return Task.CompletedTask;
    }

    [Fact]
    public Task HasDataAlign()
    {
        var cut = Render(CreateArrowInRoot());

        var div = cut.Find("div[aria-hidden='true']");
        div.GetAttribute("data-align").ShouldNotBeNull();

        return Task.CompletedTask;
    }

    [Fact]
    public Task AppliesClassValue()
    {
        var cut = Render(CreateArrowInRoot(
            classValue: _ => "arrow-class"
        ));

        var div = cut.Find("div[aria-hidden='true']");
        div.GetAttribute("class")!.ShouldContain("arrow-class");

        return Task.CompletedTask;
    }

    [Fact]
    public Task RequiresContext()
    {
        var cut = Render<NavigationMenuArrow>(parameters => parameters
            .Add(p => p.ChildContent, builder => builder.AddContent(0, "Arrow"))
        );

        cut.Markup.ShouldBeEmpty();

        return Task.CompletedTask;
    }
}
