using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorBaseUI.Field;
using BlazorBaseUI.Form;
using BlazorBaseUI.Utilities.LabelableProvider;
using BlazorBaseUI.Fieldset;
using Microsoft.Extensions.Logging;

namespace BlazorBaseUI.RadioGroup;

public sealed class RadioGroup<TValue> : ComponentBase, IReferencableComponent, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-radio.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private DotNetObjectReference<RadioGroup<TValue>>? dotNetRef;
    private bool isComponentRenderAs;
    private bool hasRendered;
    private string? defaultId;
    private string groupId = null!;
    private TValue? internalValue;
    private TValue? previousValue;
    private RadioGroupContext<TValue>? groupContext;

    private RadioGroupState state = RadioGroupState.Default;
    private bool stateDirty = true;

    private bool contextDisabled;
    private bool contextReadOnly;
    private bool contextRequired;
    private string? contextName;
    private FieldValidation? contextValidation;

    private EventCallback<FocusEventArgs> cachedFocusCallback;
    private EventCallback<FocusEventArgs> cachedBlurCallback;
    private EventCallback<KeyboardEventArgs> cachedKeyDownCaptureCallback;
    private EventCallback<ChangeEventArgs> cachedHiddenInputChangeCallback;
    private EventCallback<FocusEventArgs> cachedHiddenInputFocusCallback;
    private Func<Task> cachedValueChangedCallback = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [Inject]
    private ILogger<RadioGroup<TValue>> Logger { get; set; } = null!;

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private FormContext? FormContext { get; set; }

    [CascadingParameter]
    private LabelableContext? LabelableContext { get; set; }

    [CascadingParameter]
    private FieldsetRootContext? FieldsetContext { get; set; }

    [Parameter]
    public TValue? Value { get; set; }

    [Parameter]
    public TValue? DefaultValue { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public EventCallback<TValue?> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<RadioGroupValueChangeEventArgs<TValue>> OnValueChange { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<RadioGroupState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<RadioGroupState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool IsControlled => ValueChanged.HasDelegate;

    private TValue? CurrentValue => IsControlled ? Value : internalValue;

    private bool ResolvedDisabled => Disabled || (FieldContext?.Disabled ?? false);

    private string? ResolvedName => Name ?? FieldContext?.Name;

    private FieldRootState FieldState => FieldContext?.State ?? FieldRootState.Default;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    public RadioGroup()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        groupId = ResolvedId;

        internalValue = DefaultValue;

        var initialValue = CurrentValue;
        FieldContext?.Validation.SetInitialValue(initialValue);

        if (FieldContext is not null && initialValue is not null)
        {
            var currentValidityData = FieldContext.ValidityData;
            FieldContext.SetValidityData(currentValidityData with { Value = initialValue });
        }

        FieldContext?.SetFilled(initialValue is not null);
        FieldContext?.SubscribeFunc(this);

        previousValue = CurrentValue;

        contextDisabled = ResolvedDisabled;
        contextReadOnly = ReadOnly;
        contextRequired = Required;
        contextName = ResolvedName;
        contextValidation = FieldContext?.Validation;

        groupContext = CreateContext();

        cachedFocusCallback = EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocus);
        cachedBlurCallback = EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlur);
        cachedKeyDownCaptureCallback = EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownCapture);
        cachedHiddenInputChangeCallback = EventCallback.Factory.Create<ChangeEventArgs>(this, HandleHiddenInputChange);
        cachedHiddenInputFocusCallback = EventCallback.Factory.Create<FocusEventArgs>(this, HandleHiddenInputFocus);

        cachedValueChangedCallback = async () =>
        {
            try
            {
                await HandleValueChangedAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error handling value change in {component}", nameof(RadioGroup<>));
            }
        };
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var currentDisabled = ResolvedDisabled;
        var currentReadOnly = ReadOnly;
        var currentRequired = Required;

        if (currentDisabled != contextDisabled ||
            currentReadOnly != contextReadOnly ||
            currentRequired != contextRequired)
        {
            stateDirty = true;
        }

        if (!EqualityComparer<TValue>.Default.Equals(CurrentValue, previousValue))
        {
            previousValue = CurrentValue;
            _ = InvokeAsync(cachedValueChangedCallback);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (stateDirty)
        {
            state = RadioGroupState.FromFieldState(FieldState, ResolvedDisabled, ReadOnly, Required);
            stateDirty = false;
        }

        var needsContextUpdate = groupContext is null ||
            contextDisabled != ResolvedDisabled ||
            contextReadOnly != ReadOnly ||
            contextRequired != Required ||
            contextName != ResolvedName ||
            !ReferenceEquals(contextValidation, FieldContext?.Validation);

        if (needsContextUpdate)
        {
            contextDisabled = ResolvedDisabled;
            contextReadOnly = ReadOnly;
            contextRequired = Required;
            contextName = ResolvedName;
            contextValidation = FieldContext?.Validation;

            if (groupContext is null)
            {
                groupContext = CreateContext();
            }
            else
            {
                groupContext.Disabled = contextDisabled;
                groupContext.ReadOnly = contextReadOnly;
                groupContext.Required = contextRequired;
                groupContext.Name = contextName;
                groupContext.Validation = contextValidation;
            }
        }

        builder.OpenComponent<CascadingValue<IRadioGroupContext<TValue>>>(0);
        builder.AddComponentParameter(1, "Value", groupContext);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)RenderContent);
        builder.CloseComponent();
    }

    private void RenderContent(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "role", "radiogroup");

        if (Required)
            builder.AddAttribute(3, "aria-required", "true");

        if (ResolvedDisabled)
            builder.AddAttribute(4, "aria-disabled", "true");

        if (ReadOnly)
            builder.AddAttribute(5, "aria-readonly", "true");

        var labelledBy = LabelableContext?.LabelId ?? FieldsetContext?.LegendId;
        if (!string.IsNullOrEmpty(labelledBy))
            builder.AddAttribute(6, "aria-labelledby", labelledBy);

        var describedBy = LabelableContext?.GetAriaDescribedBy();
        if (!string.IsNullOrEmpty(describedBy))
            builder.AddAttribute(7, "aria-describedby", describedBy);

        builder.AddAttribute(8, "onfocus", cachedFocusCallback);
        builder.AddAttribute(9, "onblur", cachedBlurCallback);
        builder.AddAttribute(10, "onkeydowncapture", cachedKeyDownCaptureCallback);

        if (state.Disabled)
            builder.AddAttribute(11, "data-disabled", string.Empty);

        if (state.ReadOnly)
            builder.AddAttribute(12, "data-readonly", string.Empty);

        if (state.Required)
            builder.AddAttribute(13, "data-required", string.Empty);

        if (state.Valid == true)
            builder.AddAttribute(14, "data-valid", string.Empty);
        else if (state.Valid == false)
            builder.AddAttribute(15, "data-invalid", string.Empty);

        if (state.Touched)
            builder.AddAttribute(16, "data-touched", string.Empty);

        if (state.Dirty)
            builder.AddAttribute(17, "data-dirty", string.Empty);

        if (state.Filled)
            builder.AddAttribute(18, "data-filled", string.Empty);

        if (state.Focused)
            builder.AddAttribute(19, "data-focused", string.Empty);

        if (!string.IsNullOrEmpty(resolvedClass))
            builder.AddAttribute(20, "class", resolvedClass);

        if (!string.IsNullOrEmpty(resolvedStyle))
            builder.AddAttribute(21, "style", resolvedStyle);

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(22, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(23, component => Element = ((IReferencableComponent)component).Element);
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(24, elementReference => Element = elementReference);
            builder.AddContent(25, ChildContent);
            builder.CloseElement();
        }

        var serializedValue = SerializeValue(CurrentValue);

        builder.OpenElement(26, "input");
        builder.AddAttribute(27, "type", "radio");
        builder.AddAttribute(28, "id", groupId);
        builder.AddAttribute(29, "tabindex", -1);
        builder.AddAttribute(30, "aria-hidden", "true");
        builder.AddAttribute(31, "style", "position:absolute;pointer-events:none;opacity:0;margin:0;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0;");

        if (ResolvedName is not null)
            builder.AddAttribute(32, "name", ResolvedName);

        builder.AddAttribute(33, "value", serializedValue ?? string.Empty);

        if (CurrentValue is not null)
            builder.AddAttribute(34, "checked");

        if (ResolvedDisabled)
            builder.AddAttribute(35, "disabled");

        if (Required)
            builder.AddAttribute(36, "required");

        if (ReadOnly)
            builder.AddAttribute(37, "readonly");

        builder.AddAttribute(38, "onchange", cachedHiddenInputChangeCallback);
        builder.AddAttribute(39, "onfocus", cachedHiddenInputFocusCallback);
        builder.CloseElement();
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
        FieldContext?.UnsubscribeFunc(this);

        if (moduleTask.IsValueCreated)
        {
            try
            {
                var module = await moduleTask.Value;
                if (Element.HasValue)
                {
                    await module.InvokeVoidAsync("disposeGroup", Element.Value);
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
        stateDirty = true;
        _ = InvokeAsync(StateHasChanged);
    }

    private RadioGroupContext<TValue> CreateContext() => new(
        disabled: ResolvedDisabled,
        readOnly: ReadOnly,
        required: Required,
        name: ResolvedName,
        validation: FieldContext?.Validation,
        getCheckedValue: () => CurrentValue,
        setCheckedValue: SetValueInternalAsync,
        getGroupElement: () => Element);

    private async Task InitializeJsAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            if (Element.HasValue)
            {
                dotNetRef = DotNetObjectReference.Create(this);
                await module.InvokeVoidAsync("initializeGroup", Element.Value, dotNetRef);
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    [JSInvokable]
    public async Task OnNavigateToRadio(string serializedValue)
    {
        if (ResolvedDisabled || ReadOnly)
        {
            return;
        }

        TValue? value;
        if (typeof(TValue) == typeof(string))
        {
            value = (TValue)(object)serializedValue;
        }
        else
        {
            value = JsonSerializer.Deserialize<TValue>(serializedValue);
        }

        if (value is not null)
        {
            await SetValueInternalAsync(value);
        }
    }

    private void HandleFocus(FocusEventArgs e)
    {
        FieldContext?.SetFocused(true);
    }

    private void HandleBlur(FocusEventArgs e)
    {
        _ = HandleBlurInternalAsync();
    }

    private async Task HandleBlurInternalAsync()
    {
        if (!hasRendered)
            return;

        try
        {
            var module = await moduleTask.Value;
            var isWithinGroup = Element.HasValue && await module.InvokeAsync<bool>("isBlurWithinGroup", Element.Value);

            if (!isWithinGroup)
            {
                FieldContext?.SetTouched(true);
                FieldContext?.SetFocused(false);

                if (FieldContext?.ValidationMode == ValidationMode.OnBlur)
                {
                    await FieldContext.Validation.CommitAsync(CurrentValue);
                }
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private void HandleKeyDownCapture(KeyboardEventArgs e)
    {
        if (e.Key.StartsWith("Arrow"))
        {
            FieldContext?.SetTouched(true);
            FieldContext?.SetFocused(true);
        }
    }

    private void HandleHiddenInputChange(ChangeEventArgs e)
    {
        if (ResolvedDisabled || ReadOnly)
            return;

        FieldContext?.SetTouched(true);

        var currentValue = CurrentValue;
        var initialValue = FieldContext?.ValidityData.InitialValue;
        var isDirty = initialValue is TValue initial
            ? !EqualityComparer<TValue>.Default.Equals(currentValue, initial)
            : currentValue is not null;

        FieldContext?.SetDirty(isDirty);
        FieldContext?.SetFilled(currentValue is not null);
    }

    private void HandleHiddenInputFocus(FocusEventArgs e)
    {
        _ = FocusFirstRadioAsync();
    }

    private async Task FocusFirstRadioAsync()
    {
        if (!hasRendered || !Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            var firstRadio = await module.InvokeAsync<IJSObjectReference?>("getFirstEnabledRadio", Element.Value);
            if (firstRadio is not null)
            {
                await module.InvokeVoidAsync("focus", firstRadio);
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task SetValueInternalAsync(TValue value)
    {
        var eventArgs = new RadioGroupValueChangeEventArgs<TValue>(value);

        if (OnValueChange.HasDelegate)
        {
            await OnValueChange.InvokeAsync(eventArgs);

            if (eventArgs.IsCanceled)
            {
                StateHasChanged();
                return;
            }
        }

        if (!IsControlled)
        {
            internalValue = value;
        }

        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(value);
        }

        await HandleValueChangedAsync();
        StateHasChanged();
    }

    private async Task HandleValueChangedAsync()
    {
        var currentValue = CurrentValue;

        FormContext?.ClearErrors(ResolvedName);

        var initialValue = FieldContext?.ValidityData.InitialValue;
        var isDirty = initialValue is TValue initial
            ? !EqualityComparer<TValue>.Default.Equals(currentValue, initial)
            : currentValue is not null;

        FieldContext?.SetDirty(isDirty);
        FieldContext?.SetFilled(currentValue is not null);

        if (FieldContext?.ShouldValidateOnChangeFunc() == true)
        {
            await FieldContext.Validation.CommitAsync(currentValue);
        }
        else
        {
            await (FieldContext?.Validation.CommitAsync(currentValue, revalidateOnly: true) ?? Task.CompletedTask);
        }
    }

    private static string? SerializeValue(TValue? value)
    {
        if (value is null)
            return null;

        if (value is string str)
            return str;

        return JsonSerializer.Serialize(value);
    }
}
