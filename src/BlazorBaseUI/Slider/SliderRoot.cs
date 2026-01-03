using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using BlazorBaseUI.Field;
using BlazorBaseUI.Form;
using BlazorBaseUI.DirectionProvider;
using BlazorBaseUI.Utilities.LabelableProvider;

namespace BlazorBaseUI.Slider;

public sealed class SliderRoot : ComponentBase, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-slider.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;
    private readonly Dictionary<int, ThumbMetadata> thumbRegistry = [];

    private bool hasRendered;
    private double[] currentValues = null!;
    private double[] previousValues = null!;
    private int activeThumbIndex = -1;
    private int lastUsedThumbIndex = -1;
    private bool dragging;
    private double? indicatorStart;
    private double? indicatorEnd;
    private string? defaultId;
    private ElementReference element;
    private ElementReference controlElement;
    private ElementReference? indicatorElement;
    private SliderRootContext? cachedContext;

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
    public double Min { get; set; } = 0;

    [Parameter]
    public double Max { get; set; } = 100;

    [Parameter]
    public double Step { get; set; } = 1;

    [Parameter]
    public double LargeStep { get; set; } = 10;

    [Parameter]
    public int MinStepsBetweenValues { get; set; } = 0;

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

    [DisallowNull]
    public ElementReference? Element => element;

    private bool IsControlled => Value.HasValue || Values is not null;

    private bool IsRange => Values is not null || DefaultValues is not null || currentValues.Length > 1;

    private bool ResolvedDisabled => Disabled || (FieldContext?.Disabled ?? false);

    private string? ResolvedName => Name ?? FieldContext?.Name;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString())!;

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
        FieldContext?.Validation.SetInitialValue(initialValue);
        FieldContext?.SetFilled(currentValues.Any(v => v != Min));
        FieldContext?.RegisterFocusHandlerFunc(FocusFirstThumbAsync);
        FieldContext?.SubscribeFunc(this);

        previousValues = (double[])currentValues.Clone();
    }

    protected override void OnParametersSet()
    {
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
            previousValues = (double[])currentValues.Clone();
            HandleValuesChanged();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = State;
        var context = CreateContext(state);
        cachedContext = context;

        builder.OpenComponent<CascadingValue<ISliderRootContext>>(0);
        builder.AddComponentParameter(1, "Value", (ISliderRootContext)context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(contextBuilder =>
        {
            RenderSlider(contextBuilder, state);
        }));
        builder.CloseComponent();
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

        if (moduleTask.IsValueCreated && controlElement.Id is not null)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("dispose", controlElement);
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

    private void RenderSlider(RenderTreeBuilder builder, SliderRootState state)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var attributes = BuildSliderAttributes(state);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

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
        builder.AddElementReferenceCapture(5, e => element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    private Dictionary<string, object> BuildSliderAttributes(SliderRootState state)
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

        attributes["id"] = ResolvedId;
        attributes["role"] = "group";

        if (LabelableContext?.LabelId is not null)
            attributes["aria-labelledby"] = LabelableContext.LabelId;

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }

    private SliderRootContext CreateContext(SliderRootState state) => new(
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
        State: state,
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
        GetIndicatorPositionFunc: GetIndicatorPosition);

    private void SetActiveThumbIndex(int index)
    {
        if (activeThumbIndex != index)
        {
            activeThumbIndex = index;
            if (index >= 0)
            {
                lastUsedThumbIndex = index;
            }
            // During drag, JS controls DOM directly - don't trigger re-render
            // Re-render will happen when drag ends
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
            // When starting drag, don't trigger re-render - JS controls DOM directly
            // When ending drag, re-render to sync Blazor state with final values
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
            currentValues = newValues;
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
            currentValues = newValues;
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
            isDirty = initialValue is double initial ? Math.Abs(currentValues[0] - initial) > 1e-10 : currentValues[0] != Min;
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
        if (controlElement.Id is null)
            return;

        try
        {
            var module = await moduleTask.Value;
            var orientationStr = Orientation.ToDataAttributeString() ?? "horizontal";
            await module.InvokeVoidAsync("initialize", controlElement, null, ResolvedDisabled, ReadOnly, orientationStr);
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
