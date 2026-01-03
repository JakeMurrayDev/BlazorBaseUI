using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorBaseUI.Field;
using BlazorBaseUI.Form;
using BlazorBaseUI.Utilities.LabelableProvider;
using BlazorBaseUI.Fieldset;

namespace BlazorBaseUI.RadioGroup;

public sealed class RadioGroup<TValue> : ComponentBase, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-radio.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private string? defaultId;
    private string groupId = null!;
    private TValue? internalValue;
    private TValue? previousValue;
    private RadioGroupContext<TValue>? groupContext;

    private RadioGroupState? cachedState;
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

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

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

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private bool IsControlled => ValueChanged.HasDelegate;

    private TValue? CurrentValue => IsControlled ? Value : internalValue;

    private bool ResolvedDisabled => Disabled || (FieldContext?.Disabled ?? false);

    private string? ResolvedName => Name ?? FieldContext?.Name;

    private FieldRootState FieldState => FieldContext?.State ?? FieldRootState.Default;

    private RadioGroupState State
    {
        get
        {
            if (stateDirty || cachedState is null)
            {
                cachedState = RadioGroupState.FromFieldState(FieldState, ResolvedDisabled, ReadOnly, Required);
                stateDirty = false;
            }
            return cachedState;
        }
    }

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
    }

    protected override void OnParametersSet()
    {
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
            _ = HandleValueChangedAsync();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
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

            groupContext = groupContext is null
                ? CreateContext()
                : groupContext with
                {
                    Disabled = contextDisabled,
                    ReadOnly = contextReadOnly,
                    Required = contextRequired,
                    Name = contextName,
                    Validation = contextValidation
                };
        }

        builder.OpenComponent<CascadingValue<IRadioGroupContext<TValue>>>(0);
        builder.AddComponentParameter(1, "Value", groupContext);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(contextBuilder =>
        {
            RenderGroup(contextBuilder);
            RenderHiddenInput(contextBuilder);
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
    }

    public void NotifyStateChanged()
    {
        stateDirty = true;
        _ = InvokeAsync(StateHasChanged);
    }

    private void RenderGroup(RenderTreeBuilder builder)
    {
        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (RenderAs is not null)
        {
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, BuildAttributes(state, resolvedClass, resolvedStyle));
            builder.AddComponentParameter(2, "ChildContent", ChildContent);
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(3, tag);
        builder.AddMultipleAttributes(4, BuildAttributes(state, resolvedClass, resolvedStyle));
        builder.AddElementReferenceCapture(5, e => Element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    private void RenderHiddenInput(RenderTreeBuilder builder)
    {
        var serializedValue = SerializeValue(CurrentValue);

        builder.OpenElement(7, "input");
        builder.AddAttribute(8, "type", "radio");
        builder.AddAttribute(9, "id", groupId);
        builder.AddAttribute(10, "tabindex", -1);
        builder.AddAttribute(11, "aria-hidden", true);
        builder.AddAttribute(12, "style",
            "position:absolute;pointer-events:none;opacity:0;margin:0;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0;");

        if (ResolvedName is not null)
            builder.AddAttribute(13, "name", ResolvedName);

        builder.AddAttribute(14, "value", serializedValue ?? string.Empty);

        if (CurrentValue is not null)
            builder.AddAttribute(15, "checked", true);

        if (ResolvedDisabled)
            builder.AddAttribute(16, "disabled", true);

        if (Required)
            builder.AddAttribute(17, "required", true);

        if (ReadOnly)
            builder.AddAttribute(18, "readonly", true);

        builder.AddAttribute(19, "onchange", cachedHiddenInputChangeCallback);
        builder.AddAttribute(20, "onfocus", cachedHiddenInputFocusCallback);
        builder.CloseElement();
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
        if (groupContext is not null)
        {
            var firstRadio = groupContext.GetFirstEnabledRadio();
            if (firstRadio is not null)
            {
                await firstRadio.Focus();
            }
        }
    }

    private Dictionary<string, object> BuildAttributes(RadioGroupState state, string? resolvedClass, string? resolvedStyle)
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

        attributes["role"] = "radiogroup";

        if (Required)
            attributes["aria-required"] = true;

        if (ResolvedDisabled)
            attributes["aria-disabled"] = true;

        if (ReadOnly)
            attributes["aria-readonly"] = true;

        var labelledBy = LabelableContext?.LabelId ?? FieldsetContext?.LegendId;
        if (labelledBy is not null)
            attributes["aria-labelledby"] = labelledBy;

        var describedBy = LabelableContext?.GetAriaDescribedBy();
        if (describedBy is not null)
            attributes["aria-describedby"] = describedBy;

        attributes["onfocus"] = cachedFocusCallback;
        attributes["onblur"] = cachedBlurCallback;
        attributes["onkeydowncapture"] = cachedKeyDownCaptureCallback;

        state.WriteDataAttributes(attributes);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        return attributes;
    }

    private RadioGroupContext<TValue> CreateContext() => new(
        Disabled: ResolvedDisabled,
        ReadOnly: ReadOnly,
        Required: Required,
        Name: ResolvedName,
        Validation: FieldContext?.Validation,
        GetCheckedValue: () => CurrentValue,
        SetCheckedValue: SetValueInternalAsync);

    private async Task InitializeJsAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            if (Element.HasValue)
            {
                await module.InvokeVoidAsync("initializeGroup", Element.Value);
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
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

        return System.Text.Json.JsonSerializer.Serialize(value);
    }
}
