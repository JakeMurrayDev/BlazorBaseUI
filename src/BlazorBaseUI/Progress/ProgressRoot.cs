using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Progress;

public sealed class ProgressRoot : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private string? labelId;
    private bool isComponentRenderAs;
    private ProgressStatus previousStatus;
    private ProgressRootState state = new(ProgressStatus.Indeterminate);

    [Parameter]
    public double? Value { get; set; }

    [Parameter]
    public double Min { get; set; } = 0;

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

    public ElementReference? Element { get; private set; }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var status = ComputeStatus();
        if (status != previousStatus)
        {
            previousStatus = status;
            state = new ProgressRootState(status);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
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
            Status: state.Status,
            SetLabelIdAction: SetLabelId);

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        builder.OpenComponent<CascadingValue<ProgressRootContext>>(0);
        builder.AddAttribute(1, "Value", context);
        builder.AddAttribute(2, "IsFixed", false);
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(innerBuilder =>
        {
            if (isComponentRenderAs)
            {
                innerBuilder.OpenComponent(4, RenderAs!);
            }
            else
            {
                innerBuilder.OpenElement(4, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            }

            innerBuilder.AddMultipleAttributes(5, AdditionalAttributes);

            innerBuilder.AddAttribute(6, "role", "progressbar");
            innerBuilder.AddAttribute(7, "aria-valuemin", Min);
            innerBuilder.AddAttribute(8, "aria-valuemax", Max);

            if (Value.HasValue)
            {
                innerBuilder.AddAttribute(9, "aria-valuenow", Value.Value);
            }

            innerBuilder.AddAttribute(10, "aria-valuetext", ariaValueText);

            if (!string.IsNullOrEmpty(labelId))
            {
                innerBuilder.AddAttribute(11, "aria-labelledby", labelId);
            }

            innerBuilder.AddAttribute(12, $"data-{state.Status.ToDataAttributeString()}");

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                innerBuilder.AddAttribute(13, "class", resolvedClass);
            }
            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                innerBuilder.AddAttribute(14, "style", resolvedStyle);
            }

            if (isComponentRenderAs)
            {
                innerBuilder.AddAttribute(15, "ChildContent", ChildContent);
                innerBuilder.AddComponentReferenceCapture(16, component => { Element = ((IReferencableComponent)component).Element; });
                innerBuilder.CloseComponent();
            }
            else
            {
                innerBuilder.AddElementReferenceCapture(15, elementReference => Element = elementReference);
                innerBuilder.AddContent(16, ChildContent);
                innerBuilder.CloseElement();
            }
        }));
        builder.CloseComponent();
    }

    private ProgressStatus ComputeStatus()
    {
        if (!Value.HasValue || !double.IsFinite(Value.Value))
        {
            return ProgressStatus.Indeterminate;
        }

        return Value.Value == Max ? ProgressStatus.Complete : ProgressStatus.Progressing;
    }

    private string FormatValue(double? value)
    {
        if (!value.HasValue)
        {
            return string.Empty;
        }

        var provider = FormatProvider ?? CultureInfo.CurrentCulture;

        if (!string.IsNullOrEmpty(Format))
        {
            return value.Value.ToString(Format, provider);
        }

        var percentage = value.Value / 100.0;
        return percentage.ToString("P0", provider);
    }

    private static string GetDefaultAriaValueText(string? formattedValue, double? value)
    {
        if (!value.HasValue)
        {
            return "indeterminate progress";
        }

        return !string.IsNullOrEmpty(formattedValue) ? formattedValue : $"{value}%";
    }

    private void SetLabelId(string? id)
    {
        if (labelId == id)
        {
            return;
        }

        labelId = id;
        StateHasChanged();
    }
}
