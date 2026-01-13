using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using BlazorBaseUI.DirectionProvider;
using BlazorBaseUI.Field;
using BlazorBaseUI.Utilities.LabelableProvider;
using BlazorBaseUI.Form;

namespace BlazorBaseUI.Slider;

public sealed class SliderThumb : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "div";

    private bool hasRendered;
    private bool isComponentRenderAs;
    private string inputId = null!;
    private ElementReference element;
    private ElementReference inputElement;
    private SliderThumbState state = SliderThumbState.Default;
    private EventCallback<ChangeEventArgs> cachedOnChange;
    private EventCallback<FocusEventArgs> cachedOnFocus;
    private EventCallback<FocusEventArgs> cachedOnBlur;
    private EventCallback<KeyboardEventArgs> cachedOnKeyDown;

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

    public ElementReference? Element { get; private set; }

    private int ResolvedIndex => Index ?? 0;

    private bool IsRange => Context?.Values.Length > 1;

    private bool IsVertical => Context?.Orientation == Orientation.Vertical;

    private bool IsRtl => DirectionContext?.Direction == Direction.Rtl;

    private bool ResolvedDisabled => Disabled || (Context?.Disabled ?? false);

    private double ThumbValue => Context?.Values.ElementAtOrDefault(ResolvedIndex) ?? 0;

    private bool IsActive => Context?.ActiveThumbIndex == ResolvedIndex;

    protected override void OnInitialized()
    {
        inputId = IsRange
            ? Guid.NewGuid().ToIdString()
            : (LabelableContext?.ControlId ?? Guid.NewGuid().ToIdString());

        if (!IsRange)
        {
            LabelableContext?.SetControlId(inputId);
        }

        cachedOnChange = EventCallback.Factory.Create<ChangeEventArgs>(this, HandleChange);
        cachedOnFocus = EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocus);
        cachedOnBlur = EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlur);
        cachedOnKeyDown = EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        state = SliderThumbState.FromRootState(
            Context?.State ?? SliderRootState.Default,
            ResolvedIndex,
            IsActive);

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

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientationStr = state.Orientation.ToDataAttributeString() ?? "horizontal";
        var positionStyle = GetPositionStyle();

        var combinedStyle = string.IsNullOrEmpty(resolvedStyle)
            ? positionStyle
            : $"{resolvedStyle.TrimEnd().TrimEnd(';')}; {positionStyle}";

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "tabindex", -1);
        builder.AddAttribute(3, "data-index", state.Index.ToString());

        if (state.Dragging)
        {
            builder.AddAttribute(4, "data-dragging", string.Empty);
        }

        builder.AddAttribute(5, "data-orientation", orientationStr);

        if (state.Disabled)
        {
            builder.AddAttribute(6, "data-disabled", string.Empty);
        }

        if (state.ReadOnly)
        {
            builder.AddAttribute(7, "data-readonly", string.Empty);
        }

        if (state.Required)
        {
            builder.AddAttribute(8, "data-required", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(9, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(10, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(11, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(12, "data-dirty", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(13, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(14, "class", resolvedClass);
        }

        builder.AddAttribute(15, "style", combinedStyle);

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(16, "ChildContent", (RenderFragment)(innerBuilder =>
            {
                BuildInput(innerBuilder, 0);
                innerBuilder.AddContent(21, ChildContent);
            }));
            builder.AddComponentReferenceCapture(17, component => { element = ((IReferencableComponent)component).Element ?? default; Element = element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(18, e => { element = e; Element = e; });
            BuildInput(builder, 19);
            builder.AddContent(40, ChildContent);
            builder.CloseElement();
        }
    }

    private void BuildInput(RenderTreeBuilder builder, int startSequence)
    {
        if (Context is null)
            return;

        var thumbValue = ThumbValue;
        var formattedValue = SliderUtilities.FormatNumber(thumbValue, Context.Locale, Context.FormatOptions);
        var ariaLabel = GetAriaLabel?.Invoke(ResolvedIndex);
        var ariaValueText = GetAriaValueText?.Invoke(formattedValue, thumbValue, ResolvedIndex) ?? GetDefaultAriaValueText(formattedValue);
        var describedBy = LabelableContext?.GetAriaDescribedBy();

        builder.OpenElement(startSequence, "input");
        builder.AddAttribute(startSequence + 1, "type", "range");
        builder.AddAttribute(startSequence + 2, "id", inputId);
        builder.AddAttribute(startSequence + 3, "min", Context.Min);
        builder.AddAttribute(startSequence + 4, "max", Context.Max);
        builder.AddAttribute(startSequence + 5, "step", Context.Step);
        builder.AddAttribute(startSequence + 6, "value", thumbValue);
        builder.AddAttribute(startSequence + 7, "disabled", ResolvedDisabled);
        builder.AddAttribute(startSequence + 8, "aria-valuenow", thumbValue);
        builder.AddAttribute(startSequence + 9, "aria-orientation", Context.Orientation.ToDataAttributeString());

        if (!string.IsNullOrEmpty(ariaLabel))
        {
            builder.AddAttribute(startSequence + 10, "aria-label", ariaLabel);
        }

        if (!string.IsNullOrEmpty(ariaValueText))
        {
            builder.AddAttribute(startSequence + 11, "aria-valuetext", ariaValueText);
        }

        if (!string.IsNullOrEmpty(Context.LabelId) && !IsRange)
        {
            builder.AddAttribute(startSequence + 12, "aria-labelledby", Context.LabelId);
        }

        if (!string.IsNullOrEmpty(describedBy))
        {
            builder.AddAttribute(startSequence + 13, "aria-describedby", describedBy);
        }

        if (!string.IsNullOrEmpty(Context.Name))
        {
            var inputName = IsRange ? $"{Context.Name}[{ResolvedIndex}]" : Context.Name;
            builder.AddAttribute(startSequence + 14, "name", inputName);
        }

        builder.AddAttribute(startSequence + 15, "style", "clip: rect(0px, 0px, 0px, 0px); overflow: hidden; white-space: nowrap; position: fixed; top: 0px; left: 0px; border: 0px; padding: 0px; width: 100%; height: 100%; margin: -1px;");
        builder.AddAttribute(startSequence + 16, "onchange", cachedOnChange);
        builder.AddAttribute(startSequence + 17, "onfocus", cachedOnFocus);
        builder.AddAttribute(startSequence + 18, "onblur", cachedOnBlur);
        builder.AddAttribute(startSequence + 19, "onkeydown", cachedOnKeyDown);
        builder.AddElementReferenceCapture(startSequence + 20, e => inputElement = e);
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
            return $"position: absolute; bottom: {percent.ToString("F4", CultureInfo.InvariantCulture)}%; left: 50%; transform: translate(-50%, 50%);";
        }
        else
        {
            return $"position: absolute; top: 50%; inset-inline-start: {percent.ToString("F4", CultureInfo.InvariantCulture)}%; transform: translate(-50%, -50%);";
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
}
