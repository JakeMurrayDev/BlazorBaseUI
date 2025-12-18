using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Field;

public sealed class FieldValidity : ComponentBase
{
    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [Parameter]
    public RenderFragment<FieldValidityData>? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var validityData = FieldContext?.ValidityData ?? FieldValidityData.Default;
        builder.AddContent(0, ChildContent?.Invoke(validityData));
    }
}