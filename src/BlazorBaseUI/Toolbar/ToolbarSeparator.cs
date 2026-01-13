using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Separator;

namespace BlazorBaseUI.Toolbar;

public sealed class ToolbarSeparator : ComponentBase
{
    [CascadingParameter]
    private ToolbarRootContext? RootContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<SeparatorState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<SeparatorState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnParametersSet()
    {
        if (RootContext is null)
        {
            throw new InvalidOperationException("ToolbarSeparator must be placed within a ToolbarRoot.");
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var invertedOrientation = RootContext!.Orientation switch
        {
            Orientation.Horizontal => Orientation.Vertical,
            Orientation.Vertical => Orientation.Horizontal,
            _ => Orientation.Vertical
        };

        builder.OpenComponent<Separator.Separator>(0);
        builder.AddAttribute(1, "Orientation", invertedOrientation);

        if (!string.IsNullOrEmpty(As))
        {
            builder.AddAttribute(2, "As", As);
        }

        if (RenderAs is not null)
        {
            builder.AddAttribute(3, "RenderAs", RenderAs);
        }

        if (ClassValue is not null)
        {
            builder.AddAttribute(4, "ClassValue", ClassValue);
        }

        if (StyleValue is not null)
        {
            builder.AddAttribute(5, "StyleValue", StyleValue);
        }

        if (ChildContent is not null)
        {
            builder.AddAttribute(6, "ChildContent", ChildContent);
        }

        builder.AddMultipleAttributes(7, AdditionalAttributes);
        builder.AddComponentReferenceCapture(8, component => { Element = ((Separator.Separator)component).Element; });
        builder.CloseComponent();
    }
}
