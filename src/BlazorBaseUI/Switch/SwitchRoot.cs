using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorBaseUI.Field;
using BlazorBaseUI.Form;
using BlazorBaseUI.Utilities.LabelableProvider;

namespace BlazorBaseUI.Switch;

public sealed class SwitchRoot : ComponentBase, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "span";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-switch.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private bool isChecked;
    private bool isComponentRenderAs;
    private bool previousChecked;
    private bool previousDisabled;
    private bool previousReadOnly;
    private string? defaultId;
    private string resolvedControlId = null!;
    private string switchId = null!;
    private string inputId = null!;
    private ElementReference inputElement;
    private SwitchRootState state = SwitchRootState.Default;
    private SwitchRootContext context = SwitchRootContext.Default;
    private EventCallback<FocusEventArgs> cachedOnFocus;
    private EventCallback<FocusEventArgs> cachedOnBlur;
    private EventCallback<ChangeEventArgs> cachedOnInputChange;
    private EventCallback<FocusEventArgs> cachedOnInputFocus;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private FormContext? FormContext { get; set; }

    [CascadingParameter]
    private LabelableContext? LabelableContext { get; set; }

    [Parameter]
    public bool? Checked { get; set; }

    [Parameter]
    public bool DefaultChecked { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public string? UncheckedValue { get; set; }

    [Parameter]
    public EventCallback<bool> CheckedChanged { get; set; }

    [Parameter]
    public EventCallback<SwitchCheckedChangeEventArgs> OnCheckedChange { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<SwitchRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<SwitchRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool IsControlled => Checked.HasValue;

    private bool CurrentChecked => IsControlled ? Checked!.Value : isChecked;

    private bool ResolvedDisabled => Disabled || (FieldContext?.Disabled ?? false);

    private string? ResolvedName => Name ?? FieldContext?.Name;

    private string ResolvedControlId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    private FieldRootState FieldState => FieldContext?.State ?? FieldRootState.Default;

    public SwitchRoot()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        switchId = Guid.NewGuid().ToIdString();
        resolvedControlId = ResolvedControlId;
        LabelableContext?.SetControlId(resolvedControlId);
        inputId = resolvedControlId;

        if (!IsControlled)
        {
            isChecked = DefaultChecked;
        }

        var initialValue = CurrentChecked;
        FieldContext?.Validation.SetInitialValue(initialValue);
        FieldContext?.SetFilled(initialValue);
        FieldContext?.RegisterFocusHandlerFunc(FocusAsync);
        FieldContext?.SubscribeFunc(this);

        previousChecked = CurrentChecked;
        previousDisabled = ResolvedDisabled;
        previousReadOnly = ReadOnly;

        cachedOnFocus = EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocus);
        cachedOnBlur = EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlur);
        cachedOnInputChange = EventCallback.Factory.Create<ChangeEventArgs>(this, HandleInputChangeAsync);
        cachedOnInputFocus = EventCallback.Factory.Create<FocusEventArgs>(this, HandleInputFocusAsync);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        var newResolvedId = ResolvedControlId;
        if (newResolvedId != resolvedControlId)
        {
            resolvedControlId = newResolvedId;
            LabelableContext?.SetControlId(resolvedControlId);
        }

        UpdateCachedState();

        if (hasRendered)
        {
            if (CurrentChecked != previousChecked)
            {
                previousChecked = CurrentChecked;
                HandleCheckedChanged();
            }

            if (ResolvedDisabled != previousDisabled || ReadOnly != previousReadOnly)
            {
                previousDisabled = ResolvedDisabled;
                previousReadOnly = ReadOnly;
                _ = UpdateJsStateAsync();
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<SwitchRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)BuildInnerContent);
        builder.CloseComponent();
    }

    private void BuildInnerContent(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var labelId = LabelableContext?.LabelId;
        var describedBy = LabelableContext?.GetAriaDescribedBy();

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "id", switchId);
        builder.AddAttribute(3, "role", "switch");
        builder.AddAttribute(4, "aria-checked", CurrentChecked ? "true" : "false");
        builder.AddAttribute(5, "tabindex", ResolvedDisabled ? -1 : 0);

        if (ReadOnly)
        {
            builder.AddAttribute(6, "aria-readonly", "true");
        }

        if (!string.IsNullOrEmpty(labelId))
        {
            builder.AddAttribute(7, "aria-labelledby", labelId);
        }

        if (!string.IsNullOrEmpty(describedBy))
        {
            builder.AddAttribute(8, "aria-describedby", describedBy);
        }

        if (state.Valid == false)
        {
            builder.AddAttribute(9, "aria-invalid", "true");
        }

        builder.AddAttribute(10, "onfocus", cachedOnFocus);
        builder.AddAttribute(11, "onblur", cachedOnBlur);

        if (state.Checked)
        {
            builder.AddAttribute(12, "data-checked", string.Empty);
        }
        else
        {
            builder.AddAttribute(13, "data-unchecked", string.Empty);
        }

        if (state.Disabled)
        {
            builder.AddAttribute(14, "data-disabled", string.Empty);
        }

        if (state.ReadOnly)
        {
            builder.AddAttribute(15, "data-readonly", string.Empty);
        }

        if (state.Required)
        {
            builder.AddAttribute(16, "data-required", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(17, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(18, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(19, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(20, "data-dirty", string.Empty);
        }

        if (state.Filled)
        {
            builder.AddAttribute(21, "data-filled", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(22, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(23, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(24, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(25, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(26, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(27, elementReference => Element = elementReference);
            builder.AddContent(28, ChildContent);
            builder.CloseElement();
        }

        if (!CurrentChecked && ResolvedName is not null && UncheckedValue is not null)
        {
            builder.OpenElement(29, "input");
            builder.AddAttribute(30, "type", "hidden");
            builder.AddAttribute(31, "name", ResolvedName);
            builder.AddAttribute(32, "value", UncheckedValue);
            builder.CloseElement();
        }

        builder.OpenElement(33, "input");
        builder.AddAttribute(34, "type", "checkbox");
        builder.AddAttribute(35, "id", inputId);
        builder.AddAttribute(36, "checked", CurrentChecked);
        builder.AddAttribute(37, "disabled", ResolvedDisabled);
        builder.AddAttribute(38, "required", Required);
        builder.AddAttribute(39, "aria-hidden", "true");
        builder.AddAttribute(40, "tabindex", -1);
        builder.AddAttribute(41, "style", "position:absolute;pointer-events:none;opacity:0;margin:0;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0;");

        if (ResolvedName is not null)
        {
            builder.AddAttribute(42, "name", ResolvedName);
        }

        builder.AddAttribute(43, "onchange", cachedOnInputChange);
        builder.AddAttribute(44, "onfocus", cachedOnInputFocus);
        builder.AddElementReferenceCapture(45, elementReference => inputElement = elementReference);
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
        LabelableContext?.SetControlId(null);
        FieldContext?.UnsubscribeFunc(this);

        if (moduleTask.IsValueCreated && Element.HasValue)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("dispose", Element.Value);
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

    private void UpdateCachedState()
    {
        var newState = SwitchRootState.FromFieldState(
            FieldState,
            CurrentChecked,
            ResolvedDisabled,
            ReadOnly,
            Required);

        if (state != newState)
        {
            state = newState;
            context = new SwitchRootContext(
                Checked: CurrentChecked,
                Disabled: ResolvedDisabled,
                ReadOnly: ReadOnly,
                Required: Required,
                State: state);
        }
    }

    private async Task InitializeJsAsync()
    {
        if (!Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("initialize", Element.Value, inputElement, ResolvedDisabled, ReadOnly);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UpdateJsStateAsync()
    {
        if (!hasRendered || !Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("updateState", Element.Value, ResolvedDisabled, ReadOnly);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private void HandleFocus(FocusEventArgs e)
    {
        if (ResolvedDisabled)
        {
            return;
        }

        FieldContext?.SetFocused(true);
    }

    private void HandleBlur(FocusEventArgs e)
    {
        if (ResolvedDisabled)
        {
            return;
        }

        FieldContext?.SetTouched(true);
        FieldContext?.SetFocused(false);

        if (FieldContext?.ValidationMode == ValidationMode.OnBlur)
        {
            _ = FieldContext.Validation.CommitAsync(CurrentChecked);
        }
    }

    private async Task HandleInputChangeAsync(ChangeEventArgs e)
    {
        if (ReadOnly || ResolvedDisabled)
        {
            return;
        }

        var nextChecked = e.Value is bool b ? b : bool.TryParse(e.Value?.ToString(), out var parsed) && parsed;

        if (nextChecked == CurrentChecked)
        {
            return;
        }

        await SetChecked(nextChecked);
    }

    private async Task HandleInputFocusAsync(FocusEventArgs e)
    {
        await FocusAsync();
    }

    private async Task SetChecked(bool value)
    {
        var eventArgs = new SwitchCheckedChangeEventArgs(value);

        if (OnCheckedChange.HasDelegate)
        {
            await OnCheckedChange.InvokeAsync(eventArgs);

            if (eventArgs.IsCanceled)
            {
                await ResetInputCheckedAsync();
                return;
            }
        }

        if (!IsControlled)
        {
            isChecked = value;
        }

        if (CheckedChanged.HasDelegate)
        {
            await CheckedChanged.InvokeAsync(value);
        }

        HandleCheckedChanged();
        UpdateCachedState();
    }

    private async Task ResetInputCheckedAsync()
    {
        if (!hasRendered)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("setInputChecked", inputElement, CurrentChecked);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private void HandleCheckedChanged()
    {
        FormContext?.ClearErrors(ResolvedName);

        var initialValue = FieldContext?.ValidityData.InitialValue;
        var isDirty = initialValue is bool initial ? CurrentChecked != initial : CurrentChecked;

        FieldContext?.SetDirty(isDirty);
        FieldContext?.SetFilled(CurrentChecked);

        if (FieldContext?.ShouldValidateOnChangeFunc() == true)
        {
            _ = FieldContext.Validation.CommitAsync(CurrentChecked);
        }
        else
        {
            _ = FieldContext?.Validation.CommitAsync(CurrentChecked, revalidateOnly: true);
        }
    }

    private async ValueTask FocusAsync()
    {
        if (!hasRendered || !Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("focus", Element.Value);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }
}
