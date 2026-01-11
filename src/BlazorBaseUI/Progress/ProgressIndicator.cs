using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Progress;

public sealed class ProgressIndicator : ComponentBase
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;

    [CascadingParameter]
    private ProgressRootContext? Context { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<ProgressRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<ProgressRootState, string>? StyleValue { get; set; }

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
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
        {
            return;
        }

        var state = Context.State;
        var indicatorStyle = GetIndicatorStyle();
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var baseStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var resolvedStyle = CombineWithIndicatorStyle(baseStyle, indicatorStyle);

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);

        builder.AddAttribute(2, $"data-{state.Status.ToDataAttributeString()}");

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(3, "class", resolvedClass);
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(4, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddAttribute(5, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(6, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(5, elementReference => Element = elementReference);
            builder.AddContent(6, ChildContent);
            builder.CloseElement();
        }
    }

    private string? GetIndicatorStyle()
    {
        if (Context is null)
        {
            return null;
        }

        var value = Context.Value;
        if (!value.HasValue || !double.IsFinite(value.Value))
        {
            return null;
        }

        var percentageValue = ValueToPercent(value.Value, Context.Min, Context.Max);

        return string.Create(CultureInfo.InvariantCulture,
            $"inset-inline-start:0;height:inherit;width:{percentageValue:F4}%");
    }

    private static double ValueToPercent(double value, double min, double max)
    {
        if (max - min == 0)
        {
            return 0;
        }

        return ((value - min) / (max - min)) * 100;
    }

    private static string? CombineWithIndicatorStyle(string? baseStyle, string? indicatorStyle)
    {
        if (string.IsNullOrEmpty(indicatorStyle))
        {
            return baseStyle;
        }

        if (string.IsNullOrEmpty(baseStyle))
        {
            return indicatorStyle;
        }

        var trimmed = baseStyle.TrimEnd();
        if (!trimmed.EndsWith(';'))
        {
            trimmed += ";";
        }

        return trimmed + indicatorStyle;
    }
}
