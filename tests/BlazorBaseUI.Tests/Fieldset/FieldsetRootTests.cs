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

    private RenderFragment CreateFieldset(
        RenderFragment? childContent = null,
        RenderFragment<RenderProps<FieldsetRootState>>? render = null)
    {
        return builder =>
        {
            builder.OpenComponent<FieldsetRoot>(0);
            builder.AddAttribute(1, "ChildContent", childContent ?? ((RenderFragment)(b => b.AddContent(0, "Fieldset content"))));

            if (render is not null)
                builder.AddAttribute(2, "Render", render);

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

    [Fact]
    public Task RendersWithCustomRender()
    {
        var cut = Render(CreateFieldset(
            render: ctx => builder =>
            {
                builder.OpenElement(0, "section");
                builder.AddMultipleAttributes(1, ctx.Attributes);
                builder.AddContent(2, ctx.ChildContent);
                builder.CloseElement();
            }
        ));

        var section = cut.Find("section");
        section.ShouldNotBeNull();

        return Task.CompletedTask;
    }
}
