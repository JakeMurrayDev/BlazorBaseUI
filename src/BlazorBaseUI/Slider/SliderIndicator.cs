using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Slider;

public sealed class SliderIndicator : ComponentBase
{
    private const string DefaultTag = "div";

    private bool hasRendered;
    private ElementReference element;

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

    [DisallowNull]
    public ElementReference? Element => element;

    private bool IsVertical => Context?.Orientation == Orientation.Vertical;

    private bool IsRange => Context?.Values.Length > 1;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
            return;

        var state = Context.State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var attributes = BuildIndicatorAttributes(state);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        var indicatorStyle = GetIndicatorStyle();
        attributes["style"] = CombineStyles(
            attributes.TryGetValue("style", out var existingStyle) ? existingStyle?.ToString() : null,
            indicatorStyle);

        if (RenderAs is not null)
        {
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, attributes);
            builder.AddComponentParameter(2, "ChildContent", ChildContent);
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(3, tag);
        builder.AddMultipleAttributes(4, attributes);
        builder.AddElementReferenceCapture(5, e =>
        {
            element = e;
            Context?.SetIndicatorElement(e);
        });
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
        }
    }

    private Dictionary<string, object> BuildIndicatorAttributes(SliderRootState state)
    {
        var attributes = new Dictionary<string, object>();

        if (AdditionalAttributes is not null)
        {
            foreach (var attr in AdditionalAttributes)
            {
                if (attr.Key is not "class" and not "style")
                    attributes[attr.Key] = attr.Value;
            }
        }

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
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
                return $"position: absolute; width: inherit; bottom: 0; height: {startPercent:F4}%;";
            }

            var size = endPercent - startPercent;
            return $"position: absolute; width: inherit; bottom: {startPercent:F4}%; height: {size:F4}%;";
        }
        else
        {
            if (!IsRange)
            {
                return $"position: relative; height: inherit; inset-inline-start: 0; width: {startPercent:F4}%;";
            }

            var size = endPercent - startPercent;
            return $"position: relative; height: inherit; inset-inline-start: {startPercent:F4}%; width: {size:F4}%;";
        }
    }

    private static string CombineStyles(string? existing, string additional)
    {
        if (string.IsNullOrEmpty(existing))
            return additional;

        var trimmed = existing.TrimEnd();
        if (!trimmed.EndsWith(';'))
            trimmed += ";";

        return trimmed + " " + additional;
    }
}
