using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Separator;

public sealed class Separator : ComponentBase
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private SeparatorState state;
    private Orientation previousOrientation;

    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

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
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (previousOrientation != Orientation)
        {
            state = new SeparatorState(Orientation);
            previousOrientation = Orientation;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientationString = Orientation.ToDataAttributeString();

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(1, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(2, AdditionalAttributes);
        builder.AddAttribute(3, "role", "separator");
        builder.AddAttribute(4, "aria-orientation", orientationString);
        builder.AddAttribute(5, "data-orientation", orientationString);

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(6, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(7, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(8, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(9, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(10, elementReference => Element = elementReference);
            builder.AddContent(11, ChildContent);
            builder.CloseElement();
        }
    }
}
