using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using BlazorBaseUI.Field;
using BlazorBaseUI.Form;
using BlazorBaseUI.DirectionProvider;
using BlazorBaseUI.Utilities.LabelableProvider;

namespace BlazorBaseUI.Slider;

public sealed class SliderRoot : ComponentBase, IReferencableComponent, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-slider.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;
    private readonly Dictionary<int, ThumbMetadata> thumbRegistry = [];

    private int realtimeSubscriberCount;

    private bool hasRendered;
    private bool isComponentRenderAs;
    private double[] currentValues = null!;
    private double[] previousValues = null!;
    private int activeThumbIndex = -1;
    private int lastUsedThumbIndex = -1;
    private bool dragging;
    private double? indicatorStart;
    private double? indicatorEnd;
    private string? defaultId;
    private ElementReference? controlElement;
    private ElementReference? indicatorElement;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private FormContext? FormContext { get; set; }

    [CascadingParameter]
    private LabelableContext? LabelableContext { get; set; }

    [CascadingParameter]
    private DirectionProviderContext? DirectionContext { get; set; }

    [Parameter]
    public double? Value { get; set; }

    [Parameter]
    public double[]? Values { get; set; }

    [Parameter]
    public double? DefaultValue { get; set; }

    [Parameter]
    public double[]? DefaultValues { get; set; }

    [Parameter]
    public double Min { get; set; }

    [Parameter]
    public double Max { get; set; } = 100;

    [Parameter]
    public double Step { get; set; } = 1;

    [Parameter]
    public double LargeStep { get; set; } = 10;

    [Parameter]
    public int MinStepsBetweenValues { get; set; }

    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public ThumbCollisionBehavior ThumbCollisionBehavior { get; set; } = ThumbCollisionBehavior.Push;

    [Parameter]
    public ThumbAlignment ThumbAlignment { get; set; } = ThumbAlignment.Center;

    [Parameter]
    public NumberFormatOptions? Format { get; set; }

    [Parameter]
    public string? Locale { get; set; }

    [Parameter]
    public EventCallback<double> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<double[]> ValuesChanged { get; set; }

    [Parameter]
    public EventCallback<SliderValueChangeEventArgs<double>> OnValueChange { get; set; }

    [Parameter]
    public EventCallback<SliderValueChangeEventArgs<double[]>> OnValuesChange { get; set; }

    [Parameter]
    public EventCallback<SliderValueCommittedEventArgs<double>> OnValueCommitted { get; set; }

    [Parameter]
    public EventCallback<SliderValueCommittedEventArgs<double[]>> OnValuesCommitted { get; set; }

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

    private bool IsControlled => Value.HasValue || Values is not null;

    private bool IsRange => Values is not null || DefaultValues is not null || currentValues.Length > 1;

    private bool HasRealtimeSubscribers =>
        realtimeSubscriberCount > 0 ||
        OnValueChange.HasDelegate ||
        OnValuesChange.HasDelegate ||
        ValueChanged.HasDelegate ||
        ValuesChanged.HasDelegate;

    private bool ResolvedDisabled => Disabled || (FieldContext?.Disabled ?? false);

    private string? ResolvedName => Name ?? FieldContext?.Name;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    private FieldRootState FieldState => FieldContext?.State ?? FieldRootState.Default;

    private SliderRootState State => SliderRootState.FromFieldState(
        FieldState,
        activeThumbIndex,
        ResolvedDisabled,
        dragging,
        Max,
        Min,
        MinStepsBetweenValues,
        Orientation,
        ReadOnly,
        Required,
        Step,
        currentValues);

    public SliderRoot()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        InitializeValues();

        var initialValue = IsRange ? (object)currentValues : currentValues[0];
        const double epsilon = 1e-7;
        FieldContext?.Validation.SetInitialValue(initialValue);
        FieldContext?.SetFilled(currentValues.Any(v => Math.Abs(v - Min) > epsilon));
        FieldContext?.RegisterFocusHandlerFunc(FocusFirstThumbAsync);
        FieldContext?.SubscribeFunc(this);

        previousValues = [..currentValues];
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
            var newValues = GetControlledValues();
            if (!ValuesEqual(newValues, currentValues))
            {
                currentValues = newValues;
            }
        }

        if (hasRendered && !ValuesEqual(currentValues, previousValues))
        {
            previousValues = [..currentValues];
            HandleValuesChanged();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = CreateContext();

        builder.OpenComponent<CascadingValue<ISliderRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(innerBuilder => BuildInnerContent(innerBuilder, State)));
        builder.CloseComponent();
    }

    private void BuildInnerContent(RenderTreeBuilder builder, SliderRootState state)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientationStr = state.Orientation.ToDataAttributeString() ?? "horizontal";

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "id", ResolvedId);
        builder.AddAttribute(3, "role", "group");

        if (!string.IsNullOrEmpty(LabelableContext?.LabelId))
        {
            builder.AddAttribute(4, "aria-labelledby", LabelableContext.LabelId);
        }

        if (state.Dragging)
        {
            builder.AddAttribute(5, "data-dragging", string.Empty);
        }

        builder.AddAttribute(6, "data-orientation", orientationStr);

        if (state.Disabled)
        {
            builder.AddAttribute(7, "data-disabled", string.Empty);
        }

        if (state.ReadOnly)
        {
            builder.AddAttribute(8, "data-readonly", string.Empty);
        }

        if (state.Required)
        {
            builder.AddAttribute(9, "data-required", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(10, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(11, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(12, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(13, "data-dirty", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(14, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(15, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(16, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(17, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(18, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(19, elementReference => Element = elementReference);
            builder.AddContent(20, ChildContent);
            builder.CloseElement();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            await InitializeJsAsync();
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
                if (controlElement.HasValue)
                {
                    await module.InvokeVoidAsync("dispose", controlElement.Value);
                }
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    public void NotifyStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private void InitializeValues()
    {
        if (Value.HasValue)
        {
            currentValues = [SliderUtilities.Clamp(Value.Value, Min, Max)];
        }
        else if (Values is not null)
        {
            currentValues = Values.Select(v => SliderUtilities.Clamp(v, Min, Max)).OrderBy(v => v).ToArray();
        }
        else if (DefaultValue.HasValue)
        {
            currentValues = [SliderUtilities.Clamp(DefaultValue.Value, Min, Max)];
        }
        else if (DefaultValues is not null)
        {
            currentValues = DefaultValues.Select(v => SliderUtilities.Clamp(v, Min, Max)).OrderBy(v => v).ToArray();
        }
        else
        {
            currentValues = [Min];
        }
    }

    private double[] GetControlledValues()
    {
        if (Value.HasValue)
        {
            return [SliderUtilities.Clamp(Value.Value, Min, Max)];
        }
        if (Values is not null)
        {
            return Values.Select(v => SliderUtilities.Clamp(v, Min, Max)).OrderBy(v => v).ToArray();
        }
        return currentValues;
    }

    private static bool ValuesEqual(double[] a, double[] b)
    {
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
        {
            if (Math.Abs(a[i] - b[i]) > 1e-10) return false;
        }
        return true;
    }

    private SliderRootContext CreateContext() => new(
        ActiveThumbIndex: activeThumbIndex,
        LastUsedThumbIndex: lastUsedThumbIndex,
        ControlElement: controlElement,
        Dragging: dragging,
        Disabled: ResolvedDisabled,
        ReadOnly: ReadOnly,
        LargeStep: LargeStep,
        Max: Max,
        Min: Min,
        MinStepsBetweenValues: MinStepsBetweenValues,
        Name: ResolvedName,
        Orientation: Orientation,
        Step: Step,
        ThumbCollisionBehavior: ThumbCollisionBehavior,
        ThumbAlignment: ThumbAlignment,
        Values: currentValues,
        State: State,
        LabelId: LabelableContext?.LabelId,
        FormatOptions: Format,
        Locale: Locale,
        Validation: FieldContext?.Validation,
        SetActiveThumbIndexAction: SetActiveThumbIndex,
        SetDraggingAction: SetDragging,
        SetValueAction: SetValue,
        SetValueSilentAction: SetValueSilent,
        CommitValueAction: CommitValue,
        HandleInputChangeAction: HandleInputChange,
        RegisterThumbAction: RegisterThumb,
        UnregisterThumbAction: UnregisterThumb,
        GetThumbMetadataFunc: GetThumbMetadata,
        GetAllThumbMetadataFunc: GetAllThumbMetadata,
        SetControlElementAction: SetControlElement,
        SetIndicatorElementAction: SetIndicatorElement,
        GetIndicatorElementFunc: GetIndicatorElement,
        SetIndicatorPositionAction: SetIndicatorPosition,
        GetIndicatorPositionFunc: GetIndicatorPosition,
        HasRealtimeSubscribers: HasRealtimeSubscribers,
        RegisterRealtimeSubscriberAction: RegisterRealtimeSubscriber,
        UnregisterRealtimeSubscriberAction: UnregisterRealtimeSubscriber);

    private void SetActiveThumbIndex(int index)
    {
        if (activeThumbIndex != index)
        {
            activeThumbIndex = index;
            if (index >= 0)
            {
                lastUsedThumbIndex = index;
            }
            if (!dragging)
            {
                StateHasChanged();
            }
        }
    }

    private void SetDragging(bool value)
    {
        if (dragging != value)
        {
            dragging = value;
            if (!value)
            {
                StateHasChanged();
            }
        }
    }

    private void SetValueSilent(double[] newValues)
    {
        if (!IsControlled)
        {
            currentValues = [..newValues];
        }
    }

    private void SetValue(double[] newValues, SliderChangeReason reason, int thumbIndex)
    {
        if (ValuesEqual(newValues, currentValues))
            return;

        if (IsRange)
        {
            var eventArgs = new SliderValueChangeEventArgs<double[]>(newValues, reason, thumbIndex);
            if (OnValuesChange.HasDelegate)
            {
                _ = InvokeAsync(async () =>
                {
                    await OnValuesChange.InvokeAsync(eventArgs);
                    if (!eventArgs.IsCanceled)
                    {
                        ApplyValueChange(newValues);
                    }
                });
                return;
            }
        }
        else
        {
            var eventArgs = new SliderValueChangeEventArgs<double>(newValues[0], reason, thumbIndex);
            if (OnValueChange.HasDelegate)
            {
                _ = InvokeAsync(async () =>
                {
                    await OnValueChange.InvokeAsync(eventArgs);
                    if (!eventArgs.IsCanceled)
                    {
                        ApplyValueChange(newValues);
                    }
                });
                return;
            }
        }

        ApplyValueChange(newValues);
    }

    private void ApplyValueChange(double[] newValues)
    {
        if (!IsControlled)
        {
            currentValues = [..newValues];
        }

        if (IsRange && ValuesChanged.HasDelegate)
        {
            _ = ValuesChanged.InvokeAsync(newValues);
        }
        else if (!IsRange && ValueChanged.HasDelegate)
        {
            _ = ValueChanged.InvokeAsync(newValues[0]);
        }

        HandleValuesChanged();
        StateHasChanged();
    }

    private void CommitValue(double[] values, SliderChangeReason reason)
    {
        if (IsRange)
        {
            if (OnValuesCommitted.HasDelegate)
            {
                var eventArgs = new SliderValueCommittedEventArgs<double[]>(values, reason);
                _ = OnValuesCommitted.InvokeAsync(eventArgs);
            }
        }
        else
        {
            if (OnValueCommitted.HasDelegate)
            {
                var eventArgs = new SliderValueCommittedEventArgs<double>(values[0], reason);
                _ = OnValueCommitted.InvokeAsync(eventArgs);
            }
        }

        FieldContext?.SetTouched(true);

        if (FieldContext?.ValidationMode == ValidationMode.OnBlur)
        {
            var value = IsRange ? (object)values : values[0];
            _ = FieldContext.Validation.CommitAsync(value);
        }
    }

    private void HandleInputChange(double value, int index, SliderChangeReason reason)
    {
        var newValues = SliderUtilities.GetSliderValue(value, index, Min, Max, IsRange, currentValues);

        if (!SliderUtilities.ValidateMinimumDistance(newValues, Step, MinStepsBetweenValues))
            return;

        SetValue(newValues, reason, index);
        CommitValue(newValues, reason);
    }

    private void RegisterThumb(int index, ThumbMetadata metadata)
    {
        thumbRegistry[index] = metadata;
    }

    private void UnregisterThumb(int index)
    {
        thumbRegistry.Remove(index);
    }

    private ThumbMetadata? GetThumbMetadata(int index) =>
        thumbRegistry.TryGetValue(index, out var metadata) ? metadata : null;

    private IReadOnlyDictionary<int, ThumbMetadata> GetAllThumbMetadata() => thumbRegistry;

    private void SetControlElement(ElementReference element)
    {
        controlElement = element;
    }

    private void SetIndicatorElement(ElementReference element)
    {
        indicatorElement = element;
    }

    private ElementReference? GetIndicatorElement() => indicatorElement;

    private void SetIndicatorPosition(double? start, double? end)
    {
        indicatorStart = start;
        indicatorEnd = end;
    }

    private (double? Start, double? End) GetIndicatorPosition() => (indicatorStart, indicatorEnd);

    private void RegisterRealtimeSubscriber()
    {
        Interlocked.Increment(ref realtimeSubscriberCount);
    }

    private void UnregisterRealtimeSubscriber()
    {
        Interlocked.Decrement(ref realtimeSubscriberCount);
    }

    private void HandleValuesChanged()
    {
        FormContext?.ClearErrors(ResolvedName);

        var initialValue = FieldContext?.ValidityData.InitialValue;
        bool isDirty;

        if (IsRange)
        {
            isDirty = initialValue is double[] initial ? !ValuesEqual(currentValues, initial) : true;
        }
        else
        {
            const double epsilon = 1e-7;
            isDirty = initialValue is double initial ? Math.Abs(currentValues[0] - initial) > 1e-10 : Math.Abs(currentValues[0] - Min) > epsilon;
        }

        FieldContext?.SetDirty(isDirty);
        FieldContext?.SetFilled(currentValues.Any(v => Math.Abs(v - Min) > 1e-10));

        if (FieldContext?.ShouldValidateOnChangeFunc() == true)
        {
            var value = IsRange ? (object)currentValues : currentValues[0];
            _ = FieldContext.Validation.CommitAsync(value);
        }
        else
        {
            var value = IsRange ? (object)currentValues : currentValues[0];
            _ = FieldContext?.Validation.CommitAsync(value, revalidateOnly: true);
        }
    }

    private async Task InitializeJsAsync()
    {
        if (!controlElement.HasValue)
            return;

        try
        {
            var module = await moduleTask.Value;
            var orientationStr = Orientation.ToDataAttributeString() ?? "horizontal";
            await module.InvokeVoidAsync("initialize", controlElement.Value, null, ResolvedDisabled, ReadOnly, orientationStr);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async ValueTask FocusFirstThumbAsync()
    {
        var firstThumb = thumbRegistry.OrderBy(kvp => kvp.Key).FirstOrDefault();
        if (firstThumb.Value is null)
            return;

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("focusThumbInput", firstThumb.Value.ThumbElement);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }
}
