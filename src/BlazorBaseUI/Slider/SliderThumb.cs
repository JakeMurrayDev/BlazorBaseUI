using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using BlazorBaseUI.DirectionProvider;
using BlazorBaseUI.Field;
using BlazorBaseUI.Utilities.LabelableProvider;
using BlazorBaseUI.Form;

namespace BlazorBaseUI.Slider;

public sealed class SliderThumb : ComponentBase, IDisposable
{
    private const string DefaultTag = "div";
    private bool hasRendered;
    private string inputId = null!;
    private ElementReference element;
    private ElementReference inputElement;

    [CascadingParameter]
    private ISliderRootContext? Context { get; set; }

    [CascadingParameter]
    private DirectionProviderContext? DirectionContext { get; set; }

    [CascadingParameter]
    private LabelableContext? LabelableContext { get; set; }

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [Parameter]
    public int? Index { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public int? TabIndex { get; set; }

    [Parameter]
    public Func<int, string>? GetAriaLabel { get; set; }

    [Parameter]
    public Func<string, double, int, string>? GetAriaValueText { get; set; }

    [Parameter]
    public EventCallback<FocusEventArgs> OnFocus { get; set; }

    [Parameter]
    public EventCallback<FocusEventArgs> OnBlur { get; set; }

    [Parameter]
    public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<SliderThumbState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<SliderThumbState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private int ResolvedIndex => Index ?? 0;

    private bool IsRange => Context?.Values.Length > 1;

    private bool IsVertical => Context?.Orientation == Orientation.Vertical;

    private bool IsRtl => DirectionContext?.Direction == Direction.Rtl;

    private bool ResolvedDisabled => Disabled || (Context?.Disabled ?? false);

    private double ThumbValue => Context?.Values.ElementAtOrDefault(ResolvedIndex) ?? 0;

    private bool IsActive => Context?.ActiveThumbIndex == ResolvedIndex;

    private SliderThumbState State => SliderThumbState.FromRootState(
        Context?.State ?? SliderRootState.Default,
        ResolvedIndex,
        IsActive);

    protected override void OnInitialized()
    {
        inputId = IsRange
            ? Guid.NewGuid().ToIdString()
            : (LabelableContext?.ControlId ?? Guid.NewGuid().ToIdString());

        if (!IsRange)
        {
            LabelableContext?.SetControlId(inputId);
        }
    }

    protected override void OnParametersSet()
    {
        if (Context is not null && hasRendered)
        {
            var metadata = new ThumbMetadata(inputId, element, inputElement);
            Context.RegisterThumb(ResolvedIndex, metadata);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Context is null)
            return;

        
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(State));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(State));
        var attributes = BuildAttributes(State);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        var positionStyle = GetPositionStyle();
        attributes["style"] = CombineStyles(
            attributes.TryGetValue("style", out var existingStyle) ? existingStyle.ToString() : null,
            positionStyle);

        if (RenderAs is not null)
        {
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, attributes);
            builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                RenderInput(innerBuilder);
                innerBuilder.AddContent(3, ChildContent);
            }));
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(4, tag);
        builder.AddMultipleAttributes(5, attributes);
        builder.AddElementReferenceCapture(6, e => element = e);
        RenderInput(builder);
        builder.AddContent(7, ChildContent);
        builder.CloseElement();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            if (Context is not null)
            {
                var metadata = new ThumbMetadata(inputId, element, inputElement);
                Context.RegisterThumb(ResolvedIndex, metadata);
            }
        }
    }

    public void Dispose()
    {
        Context?.UnregisterThumb(ResolvedIndex);

        if (!IsRange)
        {
            LabelableContext?.SetControlId(null);
        }
    }

    private Dictionary<string, object> BuildAttributes(SliderThumbState state)
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

        attributes["tabindex"] = -1;

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }

    private void RenderInput(RenderTreeBuilder builder)
    {
        if (Context is null)
            return;

        var thumbValue = ThumbValue;
        var formattedValue = SliderUtilities.FormatNumber(thumbValue, Context.Locale, Context.FormatOptions);

        builder.OpenElement(8, "input");
        builder.AddAttribute(9, "type", "range");
        builder.AddAttribute(10, "id", inputId);
        builder.AddAttribute(11, "min", Context.Min);
        builder.AddAttribute(12, "max", Context.Max);
        builder.AddAttribute(13, "step", Context.Step);
        builder.AddAttribute(14, "Value", thumbValue);
        builder.AddAttribute(15, "Disabled", ResolvedDisabled);
        builder.AddAttribute(16, "aria-valuenow", thumbValue);
        builder.AddAttribute(17, "aria-Orientation", Context.Orientation.ToDataAttributeString());

        var ariaLabel = GetAriaLabel?.Invoke(ResolvedIndex);
        if (!string.IsNullOrEmpty(ariaLabel))
            builder.AddAttribute(18, "aria-label", ariaLabel);

        var ariaValueText = GetAriaValueText?.Invoke(formattedValue, thumbValue, ResolvedIndex)
            ?? GetDefaultAriaValueText(formattedValue);
        if (!string.IsNullOrEmpty(ariaValueText))
            builder.AddAttribute(19, "aria-valuetext", ariaValueText);

        if (Context.LabelId is not null && !IsRange)
            builder.AddAttribute(20, "aria-labelledby", Context.LabelId);

        var describedBy = LabelableContext?.GetAriaDescribedBy();
        if (describedBy is not null)
            builder.AddAttribute(21, "aria-describedby", describedBy);

        if (Context.Name is not null)
        {
            var inputName = IsRange ? $"{Context.Name}[{ResolvedIndex}]" : Context.Name;
            builder.AddAttribute(22, "name", inputName);
        }

        // Style matches Base-UI: position fixed, full size, clipped for screen reader accessibility
        builder.AddAttribute(23, "style", "clip: rect(0px, 0px, 0px, 0px); overflow: hidden; white-space: nowrap; position: fixed; top: 0px; left: 0px; border: 0px; padding: 0px; width: 100%; height: 100%; margin: -1px;");

        builder.AddAttribute(24, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, HandleChange));
        builder.AddAttribute(25, "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocus));
        builder.AddAttribute(26, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlur));
        builder.AddAttribute(27, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
        builder.AddElementReferenceCapture(28, e => inputElement = e);
        builder.CloseElement();
    }

    private string? GetDefaultAriaValueText(string formattedValue)
    {
        if (Context is null || ResolvedIndex < 0)
            return null;

        var values = Context.Values;
        if (values.Length == 2)
        {
            return ResolvedIndex == 0
                ? $"{formattedValue} start range"
                : $"{formattedValue} end range";
        }

        return Context.FormatOptions is not null ? formattedValue : null;
    }

    private string GetPositionStyle()
    {
        if (Context is null)
            return string.Empty;

        var percent = SliderUtilities.ValueToPercent(ThumbValue, Context.Min, Context.Max);

        if (IsVertical)
        {
            return $"position: absolute; bottom: {percent:F4}%; left: 50%; transform: translate(-50%, 50%);";
        }
        else
        {
            return $"position: absolute; top: 50%; inset-inline-start: {percent:F4}%; transform: translate(-50%, -50%);";
        }
    }

    private void HandleChange(ChangeEventArgs e)
    {
        if (Context is null || Context.ReadOnly || ResolvedDisabled)
            return;

        if (double.TryParse(e.Value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var newValue))
        {
            Context.HandleInputChange(newValue, ResolvedIndex, SliderChangeReason.InputChange);
        }
    }

    private async Task HandleFocus(FocusEventArgs e)
    {
        if (Context is null)
            return;

        Context.SetActiveThumbIndex(ResolvedIndex);
        FieldContext?.SetFocused(true);

        if (OnFocus.HasDelegate)
        {
            await OnFocus.InvokeAsync(e);
        }
    }

    private async Task HandleBlur(FocusEventArgs e)
    {
        if (Context is null)
            return;

        Context.SetActiveThumbIndex(-1);
        FieldContext?.SetTouched(true);
        FieldContext?.SetFocused(false);

        if (FieldContext?.ValidationMode == ValidationMode.OnBlur)
        {
            var value = IsRange ? (object)Context.Values : Context.Values[0];
            _ = FieldContext.Validation.CommitAsync(value);
        }

        if (OnBlur.HasDelegate)
        {
            await OnBlur.InvokeAsync(e);
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (Context is null || Context.ReadOnly || ResolvedDisabled)
            return;

        if (OnKeyDown.HasDelegate)
        {
            await OnKeyDown.InvokeAsync(e);
        }

        if (!IsSliderKey(e.Key))
            return;

        var newValue = CalculateNewValueFromKey(e);
        if (newValue.HasValue)
        {
            var newValues = SliderUtilities.GetSliderValue(
                newValue.Value, ResolvedIndex, Context.Min, Context.Max, IsRange, Context.Values);

            if (SliderUtilities.ValidateMinimumDistance(newValues, Context.Step, Context.MinStepsBetweenValues))
            {
                Context.HandleInputChange(newValue.Value, ResolvedIndex, SliderChangeReason.Keyboard);
            }
        }
    }

    private static bool IsSliderKey(string key) =>
        key is "ArrowUp" or "ArrowDown" or "ArrowLeft" or "ArrowRight"
            or "PageUp" or "PageDown" or "Home" or "End";

    private double? CalculateNewValueFromKey(KeyboardEventArgs e)
    {
        if (Context is null)
            return null;

        var thumbValue = ThumbValue;
        var step = e.ShiftKey ? Context.LargeStep : Context.Step;
        var roundedValue = SliderUtilities.RoundValueToStep(thumbValue, Context.Step, Context.Min);

        return e.Key switch
        {
            "ArrowUp" => GetNewValue(roundedValue, step, 1),
            "ArrowDown" => GetNewValue(roundedValue, step, -1),
            "ArrowRight" => GetNewValue(roundedValue, step, IsRtl ? -1 : 1),
            "ArrowLeft" => GetNewValue(roundedValue, step, IsRtl ? 1 : -1),
            "PageUp" => GetNewValue(roundedValue, Context.LargeStep, 1),
            "PageDown" => GetNewValue(roundedValue, Context.LargeStep, -1),
            "Home" => GetHomeValue(),
            "End" => GetEndValue(),
            _ => null
        };
    }

    private double GetNewValue(double currentValue, double step, int direction)
    {
        if (Context is null)
            return currentValue;

        var newValue = direction == 1
            ? Math.Min(currentValue + step, Context.Max)
            : Math.Max(currentValue - step, Context.Min);

        return newValue;
    }

    private double GetHomeValue()
    {
        if (Context is null)
            return 0;

        if (IsRange && ResolvedIndex > 0)
        {
            var previousValue = Context.Values[ResolvedIndex - 1];
            return previousValue + Context.Step * Context.MinStepsBetweenValues;
        }

        return Context.Min;
    }

    private double GetEndValue()
    {
        if (Context is null)
            return 0;

        if (IsRange && ResolvedIndex < Context.Values.Length - 1)
        {
            var nextValue = Context.Values[ResolvedIndex + 1];
            return nextValue - Context.Step * Context.MinStepsBetweenValues;
        }

        return Context.Max;
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
