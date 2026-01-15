using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Slider;

public sealed class SliderIndicator : ComponentBase, IReferencableComponent
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

    private bool IsVertical => Context?.Orientation == Orientation.Vertical;

    private bool IsRange => Context?.Values.Length > 1;

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
        var indicatorStyle = GetIndicatorStyle();

        var combinedStyle = string.IsNullOrEmpty(resolvedStyle)
            ? indicatorStyle
            : $"{resolvedStyle.TrimEnd().TrimEnd(';')}; {indicatorStyle}";

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

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

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(14, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(15, component =>
            {
                Element = ((IReferencableComponent)component).Element;
                if (Element.HasValue)
                {
                    Context?.SetIndicatorElement(Element.Value);
                }
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(16, e =>
            {
                Element = e;
                Context?.SetIndicatorElement(e);
            });
            builder.AddContent(17, ChildContent);
            builder.CloseElement();
        }
    }

    private string GetIndicatorStyle()
    {
        if (Context is null)
            return string.Empty;

        var values = Context.Values;
        var min = Context.Min;
        var max = Context.Max;

        var startPercent = SliderUtilities.ValueToPercent(values[0], min, max);
        var endPercent = IsRange
            ? SliderUtilities.ValueToPercent(values[^1], min, max)
            : startPercent;

        if (IsVertical)
        {
            if (!IsRange)
            {
                return $"position: absolute; width: inherit; bottom: 0; height: {startPercent.ToString("F4", CultureInfo.InvariantCulture)}%;";
            }

            var size = endPercent - startPercent;
            return $"position: absolute; width: inherit; bottom: {startPercent.ToString("F4", CultureInfo.InvariantCulture)}%; height: {size.ToString("F4", CultureInfo.InvariantCulture)}%;";
        }
        else
        {
            if (!IsRange)
            {
                return $"position: relative; height: inherit; inset-inline-start: 0; width: {startPercent.ToString("F4", CultureInfo.InvariantCulture)}%;";
            }

            var size = endPercent - startPercent;
            return $"position: relative; height: inherit; inset-inline-start: {startPercent.ToString("F4", CultureInfo.InvariantCulture)}%; width: {size.ToString("F4", CultureInfo.InvariantCulture)}%;";
        }
    }
}
