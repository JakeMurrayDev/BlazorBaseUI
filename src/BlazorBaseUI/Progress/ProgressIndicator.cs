using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Progress;

public sealed class ProgressIndicator : ComponentBase
{
    private const string DefaultTag = "div";

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

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
            return;

        var state = Context.State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        var attributes = BuildAttributes(state);
        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        var indicatorStyle = GetIndicatorStyle();
        if (!string.IsNullOrEmpty(indicatorStyle))
        {
            attributes["style"] = CombineStyles(
                attributes.TryGetValue("style", out var existingStyle) ? existingStyle.ToString() : null,
                indicatorStyle);
        }

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
        builder.AddElementReferenceCapture(5, e => Element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    private string? GetIndicatorStyle()
    {
        if (Context is null)
            return null;

        var value = Context.Value;
        if (!value.HasValue || !double.IsFinite(value.Value))
            return null;

        var percentageValue = ValueToPercent(value.Value, Context.Min, Context.Max);

        return string.Create(CultureInfo.InvariantCulture,
            $"inset-inline-start: 0; height: inherit; width: {percentageValue:F4}%;");
    }

    private static double ValueToPercent(double value, double min, double max)
    {
        if (max - min == 0)
            return 0;

        return ((value - min) / (max - min)) * 100;
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

    private Dictionary<string, object> BuildAttributes(ProgressRootState state)
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
}
