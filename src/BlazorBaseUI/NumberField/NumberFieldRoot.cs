using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorBaseUI.Field;
using BlazorBaseUI.Form;
using BlazorBaseUI.Slider;
using BlazorBaseUI.Utilities.LabelableProvider;

namespace BlazorBaseUI.NumberField;

public sealed class NumberFieldRoot : ComponentBase, IReferencableComponent, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-number-field.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private bool isComponentRenderAs;
    private string? defaultId;
    private double? currentValue;
    private string inputValue = string.Empty;
    private bool isScrubbing;
    private ElementReference? inputElement;
    private DotNetObjectReference<NumberFieldRoot>? dotNetRef;
    private NumberFieldRootContext context = null!;
    private NumberFieldRootState state = NumberFieldRootState.Default;

    private bool IsControlled => Value.HasValue;

    private bool ResolvedDisabled => Disabled || (FieldContext?.Disabled ?? false);

    private string? ResolvedName => Name ?? FieldContext?.Name;

    private string ResolvedId => Id ?? AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    private double MinWithDefault => Min ?? double.MinValue;

    private double MaxWithDefault => Max ?? double.MaxValue;

    private double MinWithZeroDefault => Min ?? 0;

    private FieldRootState FieldState => FieldContext?.State ?? FieldRootState.Default;

    private string InputMode
    {
        get
        {
            if (MinWithDefault >= 0)
                return "decimal";
            return "text";
        }
    }

    private CultureInfo ResolvedCulture
    {
        get
        {
            if (!string.IsNullOrEmpty(Locale))
            {
                try { return CultureInfo.GetCultureInfo(Locale); }
                catch { return CultureInfo.CurrentCulture; }
            }
            return CultureInfo.CurrentCulture;
        }
    }

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private FormContext? FormContext { get; set; }

    [CascadingParameter]
    private LabelableContext? LabelableContext { get; set; }

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public double? Min { get; set; }

    [Parameter]
    public double? Max { get; set; }

    [Parameter]
    public double SmallStep { get; set; } = 0.1;

    [Parameter]
    public double Step { get; set; } = 1;

    [Parameter]
    public double LargeStep { get; set; } = 10;

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public double? Value { get; set; }

    [Parameter]
    public double? DefaultValue { get; set; }

    [Parameter]
    public EventCallback<double?> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<NumberFieldValueChangeEventArgs> OnValueChange { get; set; }

    [Parameter]
    public EventCallback<NumberFieldValueCommittedEventArgs> OnValueCommitted { get; set; }

    [Parameter]
    public bool AllowWheelScrub { get; set; }

    [Parameter]
    public bool SnapOnStep { get; set; }

    [Parameter]
    public NumberFormatOptions? Format { get; set; }

    [Parameter]
    public string? Locale { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<NumberFieldRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<NumberFieldRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    public NumberFieldRoot()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        InitializeValue();

        FieldContext?.Validation.SetInitialValue(currentValue);
        FieldContext?.SetFilled(currentValue.HasValue);
        FieldContext?.RegisterFocusHandlerFunc(FocusInputAsync);
        FieldContext?.SubscribeFunc(this);

        context = new NumberFieldRootContext(
            setValue: SetValue,
            incrementValue: IncrementValue,
            getStepAmount: GetStepAmount,
            startAutoChange: StartAutoChange,
            stopAutoChange: StopAutoChange,
            setInputValue: SetInputValueDirect,
            setIsScrubbing: SetIsScrubbing,
            onValueCommitted: HandleValueCommitted,
            setInputElement: SetInputElement,
            focusInput: () => _ = FocusInputAsync());

        UpdateState();
        UpdateContext();
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (IsControlled)
        {
            var newValue = Value;
            if (newValue != currentValue)
            {
                currentValue = newValue;
                inputValue = FormatNumber(currentValue);
            }
        }

        UpdateState();
        UpdateContext();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<INumberFieldRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(innerBuilder => BuildInnerContent(innerBuilder, state)));
        builder.CloseComponent();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            dotNetRef = DotNetObjectReference.Create(this);

            if (inputElement.HasValue && AllowWheelScrub)
            {
                await RegisterWheelListenerAsync();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        LabelableContext?.SetControlId(null);
        FieldContext?.UnsubscribeFunc(this);

        if (moduleTask.IsValueCreated)
        {
            try
            {
                var module = await moduleTask.Value;
                if (inputElement.HasValue)
                {
                    await module.InvokeVoidAsync("dispose", inputElement.Value);
                }
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }

        dotNetRef?.Dispose();
    }

    public void NotifyStateChanged()
    {
        UpdateState();
        UpdateContext();
        _ = InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public void OnWheelChange(int direction, bool altKey, bool shiftKey)
    {
        if (ResolvedDisabled || ReadOnly) return;

        var amount = GetStepAmount(altKey, shiftKey);
        IncrementValue(amount, direction, NumberFieldChangeReason.Wheel);
        HandleValueCommitted(currentValue, NumberFieldChangeReason.Wheel);
    }

    [JSInvokable]
    public void OnAutoChangeTick(bool isIncrement)
    {
        if (ResolvedDisabled || ReadOnly) return;

        var amount = Step;
        IncrementValue(amount, isIncrement ? 1 : -1, isIncrement ? NumberFieldChangeReason.IncrementPress : NumberFieldChangeReason.DecrementPress);
    }

    [JSInvokable]
    public void OnAutoChangeEnd(bool isIncrement)
    {
        HandleValueCommitted(currentValue, isIncrement ? NumberFieldChangeReason.IncrementPress : NumberFieldChangeReason.DecrementPress);
    }

    private void BuildInnerContent(RenderTreeBuilder builder, NumberFieldRootState currentState)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(currentState));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(currentState));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);

            if (currentState.Scrubbing)
            {
                builder.AddAttribute(2, "data-scrubbing", string.Empty);
            }

            if (currentState.Disabled)
            {
                builder.AddAttribute(3, "data-disabled", string.Empty);
            }

            if (currentState.ReadOnly)
            {
                builder.AddAttribute(4, "data-readonly", string.Empty);
            }

            if (currentState.Required)
            {
                builder.AddAttribute(5, "data-required", string.Empty);
            }

            if (currentState.Valid == true)
            {
                builder.AddAttribute(6, "data-valid", string.Empty);
            }
            else if (currentState.Valid == false)
            {
                builder.AddAttribute(7, "data-invalid", string.Empty);
            }

            if (currentState.Touched)
            {
                builder.AddAttribute(8, "data-touched", string.Empty);
            }

            if (currentState.Dirty)
            {
                builder.AddAttribute(9, "data-dirty", string.Empty);
            }

            if (currentState.Filled)
            {
                builder.AddAttribute(10, "data-filled", string.Empty);
            }

            if (currentState.Focused)
            {
                builder.AddAttribute(11, "data-focused", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(12, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(13, "style", resolvedStyle);
            }

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

            if (currentState.Scrubbing)
            {
                builder.AddAttribute(2, "data-scrubbing", string.Empty);
            }

            if (currentState.Disabled)
            {
                builder.AddAttribute(3, "data-disabled", string.Empty);
            }

            if (currentState.ReadOnly)
            {
                builder.AddAttribute(4, "data-readonly", string.Empty);
            }

            if (currentState.Required)
            {
                builder.AddAttribute(5, "data-required", string.Empty);
            }

            if (currentState.Valid == true)
            {
                builder.AddAttribute(6, "data-valid", string.Empty);
            }
            else if (currentState.Valid == false)
            {
                builder.AddAttribute(7, "data-invalid", string.Empty);
            }

            if (currentState.Touched)
            {
                builder.AddAttribute(8, "data-touched", string.Empty);
            }

            if (currentState.Dirty)
            {
                builder.AddAttribute(9, "data-dirty", string.Empty);
            }

            if (currentState.Filled)
            {
                builder.AddAttribute(10, "data-filled", string.Empty);
            }

            if (currentState.Focused)
            {
                builder.AddAttribute(11, "data-focused", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(12, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(13, "style", resolvedStyle);
            }

            builder.AddElementReferenceCapture(14, elementReference => Element = elementReference);
            builder.AddContent(15, ChildContent);
            builder.CloseElement();
            builder.CloseRegion();
        }

        builder.OpenRegion(2);
        builder.OpenElement(0, "input");
        builder.AddAttribute(1, "type", "number");
        builder.AddAttribute(2, "name", ResolvedName);
        builder.AddAttribute(3, "value", currentValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
        if (Min.HasValue) builder.AddAttribute(4, "min", Min.Value);
        if (Max.HasValue) builder.AddAttribute(5, "max", Max.Value);
        builder.AddAttribute(6, "step", Step);
        builder.AddAttribute(7, "disabled", ResolvedDisabled);
        builder.AddAttribute(8, "required", Required);
        builder.AddAttribute(9, "aria-hidden", "true");
        builder.AddAttribute(10, "tabindex", -1);
        builder.AddAttribute(11, "style", "position:absolute;width:1px;height:1px;padding:0;margin:-1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0");
        builder.CloseElement();
        builder.CloseRegion();
    }

    private void InitializeValue()
    {
        if (Value.HasValue)
        {
            currentValue = ClampValue(Value.Value);
        }
        else if (DefaultValue.HasValue)
        {
            currentValue = ClampValue(DefaultValue.Value);
        }
        else
        {
            currentValue = null;
        }

        inputValue = FormatNumber(currentValue);
    }

    private void SetValue(double? unvalidatedValue, NumberFieldChangeReason reason, int? direction)
    {
        var validatedValue = ValidateNumber(unvalidatedValue, direction);

        if (validatedValue != currentValue || unvalidatedValue != currentValue)
        {
            var eventArgs = new NumberFieldValueChangeEventArgs(validatedValue, reason, direction);

            if (OnValueChange.HasDelegate)
            {
                _ = InvokeAsync(async () =>
                {
                    await OnValueChange.InvokeAsync(eventArgs);
                    if (!eventArgs.IsCanceled)
                    {
                        ApplyValueChange(validatedValue, reason);
                    }
                });
                return;
            }

            ApplyValueChange(validatedValue, reason);
        }
    }

    private void ApplyValueChange(double? newValue, NumberFieldChangeReason reason)
    {
        if (!IsControlled)
        {
            currentValue = newValue;
        }

        if (ValueChanged.HasDelegate)
        {
            _ = ValueChanged.InvokeAsync(newValue);
        }

        inputValue = FormatNumber(newValue);
        HandleValuesChanged();
        UpdateState();
        UpdateContext();
        StateHasChanged();
    }

    private void IncrementValue(double amount, int direction, NumberFieldChangeReason reason)
    {
        var prevValue = currentValue ?? Math.Max(0, Min ?? 0);
        var nextValue = prevValue + amount * direction;
        SetValue(nextValue, reason, direction);
    }

    private double GetStepAmount(bool altKey, bool shiftKey)
    {
        if (altKey) return SmallStep;
        if (shiftKey) return LargeStep;
        return Step;
    }

    private void StartAutoChange(bool isIncrement)
    {
        if (!hasRendered || !inputElement.HasValue || dotNetRef is null) return;

        _ = InvokeAsync(async () =>
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("startAutoChange", inputElement.Value, dotNetRef, isIncrement, Step, Min ?? double.MinValue, Max ?? double.MaxValue);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        });
    }

    private void StopAutoChange()
    {
        if (!hasRendered || !inputElement.HasValue) return;

        _ = InvokeAsync(async () =>
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("stopAutoChange", inputElement.Value);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        });
    }

    private void SetInputValueDirect(string value)
    {
        inputValue = value;
        UpdateState();
        UpdateContext();
        StateHasChanged();
    }

    private void SetIsScrubbing(bool value)
    {
        if (isScrubbing != value)
        {
            isScrubbing = value;
            UpdateState();
            UpdateContext();
            StateHasChanged();
        }
    }

    private void HandleValueCommitted(double? value, NumberFieldChangeReason reason)
    {
        if (OnValueCommitted.HasDelegate)
        {
            var eventArgs = new NumberFieldValueCommittedEventArgs(value, reason);
            _ = OnValueCommitted.InvokeAsync(eventArgs);
        }

        FieldContext?.SetTouched(true);

        if (FieldContext?.ValidationMode == ValidationMode.OnBlur)
        {
            _ = FieldContext.Validation.CommitAsync(value);
        }
    }

    private void SetInputElement(ElementReference element)
    {
        inputElement = element;

        if (hasRendered && AllowWheelScrub)
        {
            _ = RegisterWheelListenerAsync();
        }
    }

    private async Task RegisterWheelListenerAsync()
    {
        if (!inputElement.HasValue || dotNetRef is null) return;

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("registerWheelListener", inputElement.Value, dotNetRef, ResolvedDisabled, ReadOnly);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async ValueTask FocusInputAsync()
    {
        if (!inputElement.HasValue) return;

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("focusInput", inputElement.Value);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private void HandleValuesChanged()
    {
        FormContext?.ClearErrors(ResolvedName);

        var initialValue = FieldContext?.ValidityData.InitialValue;
        var isDirty = initialValue is double initial ? currentValue != initial : currentValue.HasValue;

        FieldContext?.SetDirty(isDirty);
        FieldContext?.SetFilled(currentValue.HasValue);

        if (FieldContext?.ShouldValidateOnChangeFunc() == true)
        {
            _ = FieldContext.Validation.CommitAsync(currentValue);
        }
        else
        {
            _ = FieldContext?.Validation.CommitAsync(currentValue, revalidateOnly: true);
        }
    }

    private double? ValidateNumber(double? value, int? direction)
    {
        if (!value.HasValue) return null;

        var clamped = ClampValue(value.Value);

        if (SnapOnStep && direction.HasValue)
        {
            var step = direction.Value * Step;
            if (step != 0)
            {
                var baseVal = MinWithZeroDefault;
                if (MinWithDefault != double.MinValue)
                {
                    baseVal = MinWithDefault;
                }
                clamped = SnapToStep(clamped, baseVal, step);
            }
        }

        return RemoveFloatingPointErrors(clamped);
    }

    private double ClampValue(double value)
    {
        return Math.Max(MinWithDefault, Math.Min(MaxWithDefault, value));
    }

    private static double SnapToStep(double value, double baseVal, double step)
    {
        if (step == 0) return value;

        var stepSize = Math.Abs(step);
        var direction = Math.Sign(step);
        var rawSteps = (value - baseVal) / stepSize;

        int snappedSteps;
        if (direction > 0)
            snappedSteps = (int)Math.Floor(rawSteps);
        else
            snappedSteps = (int)Math.Ceiling(rawSteps);

        return baseVal + snappedSteps * stepSize;
    }

    private static double RemoveFloatingPointErrors(double value)
    {
        return Math.Round(value * 1e10) / 1e10;
    }

    private string FormatNumber(double? value)
    {
        if (!value.HasValue) return string.Empty;

        var culture = ResolvedCulture;

        if (Format is not null)
        {
            var formatString = GetFormatString();
            return value.Value.ToString(formatString, culture);
        }

        return value.Value.ToString("G", culture);
    }

    private string GetFormatString()
    {
        if (Format is null) return "G";

        var style = Format.Style?.ToLowerInvariant();
        var minFrac = Format.MinimumFractionDigits ?? 0;
        var maxFrac = Format.MaximumFractionDigits ?? 20;

        return style switch
        {
            "currency" => $"C{minFrac}",
            "percent" => $"P{minFrac}",
            _ => maxFrac > 0 ? $"N{Math.Min(minFrac, maxFrac)}" : "N0"
        };
    }

    private void UpdateState()
    {
        state = NumberFieldRootState.FromFieldState(
            FieldState,
            currentValue,
            inputValue,
            Required,
            ResolvedDisabled,
            ReadOnly,
            isScrubbing);
    }

    private void UpdateContext()
    {
        context.Update(
            inputValue: inputValue,
            value: currentValue,
            minWithDefault: MinWithDefault,
            maxWithDefault: MaxWithDefault,
            min: Min,
            max: Max,
            disabled: ResolvedDisabled,
            readOnly: ReadOnly,
            id: ResolvedId,
            name: ResolvedName,
            required: Required,
            invalid: FieldContext?.Invalid,
            inputMode: InputMode,
            isScrubbing: isScrubbing,
            locale: Locale,
            formatOptions: Format,
            state: state,
            inputElement: inputElement);
    }
}
