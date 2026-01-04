using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Progress;

public sealed class ProgressRoot : ComponentBase
{
    private const string DefaultTag = "div";

    private string? labelId;

    [Parameter]
    public double? Value { get; set; }

    [Parameter]
    public double Min { get; set; }

    [Parameter]
    public double Max { get; set; } = 100;

    [Parameter]
    public string? Format { get; set; }

    [Parameter]
    public IFormatProvider? FormatProvider { get; set; }

    [Parameter]
    public Func<string?, double?, string>? GetAriaValueText { get; set; }

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
        var status = ComputeStatus();
        var state = new ProgressRootState(status);
        var formattedValue = FormatValue(Value);
        var ariaValueText = GetAriaValueText is not null
            ? GetAriaValueText(formattedValue, Value)
            : GetDefaultAriaValueText(formattedValue, Value);

        var context = new ProgressRootContext(
            FormattedValue: formattedValue,
            Max: Max,
            Min: Min,
            Value: Value,
            State: state,
            Status: status,
            SetLabelIdAction: SetLabelId);

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        var attributes = BuildAttributes(state, ariaValueText);
        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        builder.OpenComponent<CascadingValue<ProgressRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(contentBuilder =>
        {
            if (RenderAs is not null)
            {
                contentBuilder.OpenComponent(4, RenderAs);
                contentBuilder.AddMultipleAttributes(5, attributes);
                contentBuilder.AddComponentParameter(6, "ChildContent", ChildContent);
                contentBuilder.CloseComponent();
            }
            else
            {
                var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
                contentBuilder.OpenElement(7, tag);
                contentBuilder.AddMultipleAttributes(8, attributes);
                contentBuilder.AddElementReferenceCapture(9, e => Element = e);
                contentBuilder.AddContent(10, ChildContent);
                contentBuilder.CloseElement();
            }
        }));
        builder.CloseComponent();
    }

    private ProgressStatus ComputeStatus()
    {
        if (!Value.HasValue || !double.IsFinite(Value.Value))
            return ProgressStatus.Indeterminate;

        return Value.Value >= Max ? ProgressStatus.Complete : ProgressStatus.Progressing;
    }

    private string FormatValue(double? value)
    {
        if (!value.HasValue)
            return string.Empty;

        var provider = FormatProvider ?? CultureInfo.CurrentCulture;

        if (!string.IsNullOrEmpty(Format))
            return value.Value.ToString(Format, provider);

        var percentage = value.Value / 100.0;
        return percentage.ToString("P0", provider);
    }

    private static string GetDefaultAriaValueText(string? formattedValue, double? value)
    {
        if (!value.HasValue)
            return "indeterminate progress";

        return !string.IsNullOrEmpty(formattedValue) ? formattedValue : $"{value}%";
    }

    private Dictionary<string, object> BuildAttributes(ProgressRootState state, string ariaValueText)
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

        attributes["role"] = "progressbar";
        attributes["aria-valuemin"] = Min;
        attributes["aria-valuemax"] = Max;

        if (Value.HasValue)
            attributes["aria-valuenow"] = Value.Value;

        attributes["aria-valuetext"] = ariaValueText;

        if (!string.IsNullOrEmpty(labelId))
            attributes["aria-labelledby"] = labelId;

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }

    private void SetLabelId(string? id)
    {
        if (labelId == id) return;
        labelId = id;
        StateHasChanged();
    }
}
