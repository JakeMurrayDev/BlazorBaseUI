using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Slider;

public sealed class SliderTrack : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private SliderRootState state = SliderRootState.Default;

    [CascadingParameter]
    private ISliderRootContext? Context { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<SliderRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<SliderRootState, string>? StyleValue { get; set; }

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

        if (Context is not null)
        {
            state = Context.State;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
            return;

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientationStr = state.Orientation.ToDataAttributeString() ?? "horizontal";

        var baseStyle = "position: relative;";
        var combinedStyle = string.IsNullOrEmpty(resolvedStyle) ? baseStyle : $"{resolvedStyle.TrimEnd().TrimEnd(';')}; {baseStyle}";

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);

            if (state.Dragging)
            {
                builder.AddAttribute(2, "data-dragging", string.Empty);
            }

            builder.AddAttribute(3, "data-orientation", orientationStr);

            if (state.Disabled)
            {
                builder.AddAttribute(4, "data-disabled", string.Empty);
            }

            if (state.ReadOnly)
            {
                builder.AddAttribute(5, "data-readonly", string.Empty);
            }

            if (state.Required)
            {
                builder.AddAttribute(6, "data-required", string.Empty);
            }

            if (state.Valid == true)
            {
                builder.AddAttribute(7, "data-valid", string.Empty);
            }
            else if (state.Valid == false)
            {
                builder.AddAttribute(8, "data-invalid", string.Empty);
            }

            if (state.Touched)
            {
                builder.AddAttribute(9, "data-touched", string.Empty);
            }

            if (state.Dirty)
            {
                builder.AddAttribute(10, "data-dirty", string.Empty);
            }

            if (state.Focused)
            {
                builder.AddAttribute(11, "data-focused", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(12, "class", resolvedClass);
            }

            builder.AddAttribute(13, "style", combinedStyle);
            builder.AddComponentParameter(14, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(15, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);

            if (state.Dragging)
            {
                builder.AddAttribute(2, "data-dragging", string.Empty);
            }

            builder.AddAttribute(3, "data-orientation", orientationStr);

            if (state.Disabled)
            {
                builder.AddAttribute(4, "data-disabled", string.Empty);
            }

            if (state.ReadOnly)
            {
                builder.AddAttribute(5, "data-readonly", string.Empty);
            }

            if (state.Required)
            {
                builder.AddAttribute(6, "data-required", string.Empty);
            }

            if (state.Valid == true)
            {
                builder.AddAttribute(7, "data-valid", string.Empty);
            }
            else if (state.Valid == false)
            {
                builder.AddAttribute(8, "data-invalid", string.Empty);
            }

            if (state.Touched)
            {
                builder.AddAttribute(9, "data-touched", string.Empty);
            }

            if (state.Dirty)
            {
                builder.AddAttribute(10, "data-dirty", string.Empty);
            }

            if (state.Focused)
            {
                builder.AddAttribute(11, "data-focused", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(12, "class", resolvedClass);
            }

            builder.AddAttribute(13, "style", combinedStyle);
            builder.AddElementReferenceCapture(14, elementReference => Element = elementReference);
            builder.AddContent(15, ChildContent);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }
}
