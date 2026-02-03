using BlazorBaseUI.Fieldset;
using BlazorBaseUI.Tests.Contracts.Fieldset;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tests.Fieldset;

public class FieldsetRootTests : BunitContext, IFieldsetRootContract
{
    public FieldsetRootTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private RenderFragment CreateFieldset(RenderFragment? childContent = null)
    {
        return builder =>
        {
            builder.OpenComponent<FieldsetRoot>(0);
            builder.AddAttribute(1, "ChildContent", childContent ?? ((RenderFragment)(b => b.AddContent(0, "Fieldset content"))));
            builder.CloseComponent();
        };
    }

    [Fact]
    public Task RendersAsFieldsetByDefault()
    {
        var cut = Render(CreateFieldset());
        var fieldset = cut.Find("fieldset");
        fieldset.ShouldNotBeNull();
        fieldset.TagName.ShouldBe("FIELDSET");
        return Task.CompletedTask;
    }
}
