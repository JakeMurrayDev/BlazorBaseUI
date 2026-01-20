using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorBaseUI.Field;
using BlazorBaseUI.RadioGroup;
using BlazorBaseUI.Utilities.LabelableProvider;
using BlazorBaseUI.Form;

namespace BlazorBaseUI.Radio;

public sealed class RadioRoot<TValue> : ComponentBase, IReferencableComponent, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "span";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-radio.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool isComponentRenderAs;
    private bool hasRendered;
    private bool previousDisabled;
    private bool previousReadOnly;
    private bool previousChecked;
    private string? defaultId;
    private string resolvedControlId = null!;
    private string radioId = null!;
    private string inputId = null!;
    private ElementReference inputElement;

    private RadioRootState state = RadioRootState.Default;
    private RadioRootContext? cachedContext;
    private bool stateDirty = true;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private FieldItemContext? FieldItemContext { get; set; }

    [CascadingParameter]
    private LabelableContext? LabelableContext { get; set; }

    [CascadingParameter]
    private IRadioGroupContext<TValue>? GroupContext { get; set; }

    [Parameter, EditorRequired]
    public TValue Value { get; set; } = default!;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Required { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<RadioRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<RadioRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool IsInGroup => GroupContext is not null;

    private bool CurrentChecked
    {
        get
        {
            if (GroupContext is not null)
                return EqualityComparer<TValue>.Default.Equals(GroupContext.CheckedValue, Value);

            return false;
        }
    }

    private bool ResolvedDisabled =>
        Disabled ||
        (FieldContext?.Disabled ?? false) ||
        (FieldItemContext?.Disabled ?? false) ||
        (GroupContext?.Disabled ?? false);

    private bool ResolvedReadOnly =>
        ReadOnly ||
        (GroupContext?.ReadOnly ?? false);

    private bool ResolvedRequired =>
        Required ||
        (GroupContext?.Required ?? false);

    private string? ResolvedName => Name ?? GroupContext?.Name ?? FieldContext?.Name;

    private string ResolvedControlId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    private FieldRootState FieldState => FieldContext?.State ?? FieldRootState.Default;

    public RadioRoot()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        radioId = Guid.NewGuid().ToIdString();
        resolvedControlId = ResolvedControlId;
        inputId = resolvedControlId;

        LabelableContext?.SetControlId(resolvedControlId);

        FieldContext?.RegisterFocusHandlerFunc(FocusAsync);
        FieldContext?.SubscribeFunc(this);

        previousDisabled = ResolvedDisabled;
        previousReadOnly = ResolvedReadOnly;
        previousChecked = CurrentChecked;
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var newResolvedId = ResolvedControlId;
        if (newResolvedId != resolvedControlId)
        {
            resolvedControlId = newResolvedId;
            inputId = resolvedControlId;
            LabelableContext?.SetControlId(resolvedControlId);
        }

        var currentDisabled = ResolvedDisabled;
        var currentReadOnly = ResolvedReadOnly;
        var currentChecked = CurrentChecked;

        if (currentDisabled != previousDisabled ||
            currentReadOnly != previousReadOnly ||
            currentChecked != previousChecked)
        {
            stateDirty = true;
        }

        if (hasRendered)
        {
            if (currentDisabled != previousDisabled || currentReadOnly != previousReadOnly)
            {
                previousDisabled = currentDisabled;
                previousReadOnly = currentReadOnly;
                _ = UpdateJsStateAsync();
            }
        }

        previousDisabled = currentDisabled;
        previousReadOnly = currentReadOnly;
        previousChecked = currentChecked;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (stateDirty)
        {
            state = RadioRootState.FromFieldState(
                FieldState,
                CurrentChecked,
                ResolvedDisabled,
                ResolvedReadOnly,
                ResolvedRequired);
            stateDirty = false;
        }

        if (cachedContext is null ||
            cachedContext.Checked != state.Checked ||
            cachedContext.Disabled != state.Disabled ||
            cachedContext.ReadOnly != state.ReadOnly ||
            cachedContext.Required != state.Required ||
            !ReferenceEquals(cachedContext.State, state))
        {
            cachedContext = new RadioRootContext(
                Checked: CurrentChecked,
                Disabled: ResolvedDisabled,
                ReadOnly: ResolvedReadOnly,
                Required: ResolvedRequired,
                State: state);
        }

        builder.OpenComponent<CascadingValue<RadioRootContext>>(0);
        builder.AddComponentParameter(1, "Value", cachedContext);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)RenderContent);
        builder.CloseComponent();
    }

    private void RenderContent(RenderTreeBuilder builder)
    {
        var resolvedRootId = AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => radioId);
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
        builder.AddAttribute(2, "id", resolvedRootId);
        builder.AddAttribute(3, "role", "radio");
        builder.AddAttribute(4, "aria-checked", CurrentChecked ? "true" : "false");

        if (IsInGroup)
        {
            var isActiveItem = CurrentChecked || GroupContext!.CheckedValue is null;
            builder.AddAttribute(5, "tabindex", ResolvedDisabled ? -1 : (isActiveItem ? 0 : -1));
            builder.AddAttribute(6, "data-radio-item", string.Empty);
        }
        else
        {
            builder.AddAttribute(7, "tabindex", ResolvedDisabled ? -1 : 0);
        }

        if (ResolvedReadOnly)
            builder.AddAttribute(8, "aria-readonly", "true");

        if (ResolvedRequired)
            builder.AddAttribute(9, "aria-required", "true");

        if (LabelableContext?.LabelId is not null)
            builder.AddAttribute(10, "aria-labelledby", LabelableContext.LabelId);

        var describedBy = LabelableContext?.GetAriaDescribedBy();
        if (!string.IsNullOrEmpty(describedBy))
            builder.AddAttribute(11, "aria-describedby", describedBy);

        if (state.Valid == false)
            builder.AddAttribute(12, "aria-invalid", "true");

        builder.AddAttribute(13, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));
        builder.AddAttribute(14, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));
        builder.AddAttribute(15, "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocus));
        builder.AddAttribute(16, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlurAsync));

        if (state.Checked)
            builder.AddAttribute(17, "data-checked", string.Empty);
        else
            builder.AddAttribute(18, "data-unchecked", string.Empty);

        if (state.Disabled)
            builder.AddAttribute(19, "data-disabled", string.Empty);

        if (state.ReadOnly)
            builder.AddAttribute(20, "data-readonly", string.Empty);

        if (state.Required)
            builder.AddAttribute(21, "data-required", string.Empty);

        if (state.Valid == true)
            builder.AddAttribute(22, "data-valid", string.Empty);
        else if (state.Valid == false)
            builder.AddAttribute(23, "data-invalid", string.Empty);

        if (state.Touched)
            builder.AddAttribute(24, "data-touched", string.Empty);

        if (state.Dirty)
            builder.AddAttribute(25, "data-dirty", string.Empty);

        if (state.Filled)
            builder.AddAttribute(26, "data-filled", string.Empty);

        if (state.Focused)
            builder.AddAttribute(27, "data-focused", string.Empty);

        if (!string.IsNullOrEmpty(resolvedClass))
            builder.AddAttribute(28, "class", resolvedClass);

        if (!string.IsNullOrEmpty(resolvedStyle))
            builder.AddAttribute(29, "style", resolvedStyle);

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(30, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(31, component => Element = ((IReferencableComponent)component).Element);
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(32, elementReference => Element = elementReference);
            builder.AddContent(33, ChildContent);
            builder.CloseElement();
        }

        builder.OpenElement(34, "input");
        builder.AddAttribute(35, "type", "radio");
        builder.AddAttribute(36, "id", inputId);
        builder.AddAttribute(37, "tabindex", -1);
        builder.AddAttribute(38, "aria-hidden", "true");
        builder.AddAttribute(39, "style", "position:absolute;pointer-events:none;opacity:0;margin:0;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0;");

        if (CurrentChecked)
            builder.AddAttribute(40, "checked");

        if (ResolvedDisabled)
            builder.AddAttribute(41, "disabled");

        if (ResolvedRequired)
            builder.AddAttribute(42, "required");

        if (ResolvedName is not null)
            builder.AddAttribute(43, "name", ResolvedName);

        if (Value is not null)
            builder.AddAttribute(44, "value", SerializeValue(Value));

        builder.AddAttribute(45, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, HandleInputChangeAsync));
        builder.AddAttribute(46, "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, HandleInputFocusAsync));
        builder.AddElementReferenceCapture(47, e => inputElement = e);
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Element.HasValue)
        {
            hasRendered = true;
            await InitializeJsAsync();
            await RegisterWithGroupAsync();
        }
    }

    private async Task RegisterWithGroupAsync()
    {
        if (!IsInGroup || !Element.HasValue || !GroupContext!.GroupElement.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            var serializedValue = SerializeValue(Value);
            await module.InvokeVoidAsync("registerRadio", GroupContext.GroupElement.Value, Element.Value, serializedValue);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UnregisterFromGroupAsync()
    {
        if (!IsInGroup || !Element.HasValue || !GroupContext!.GroupElement.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("unregisterRadio", GroupContext.GroupElement.Value, Element.Value);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
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
                await UnregisterFromGroupAsync();
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
        stateDirty = true;
        _ = InvokeAsync(StateHasChanged);
    }

    internal async ValueTask FocusAsync()
    {
        if (!hasRendered || !Element.HasValue)
            return;

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("focus", Element.Value);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task InitializeJsAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("initialize", Element, inputElement, ResolvedDisabled, ResolvedReadOnly);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UpdateJsStateAsync()
    {
        if (!hasRendered)
            return;

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("updateState", Element, inputElement, ResolvedDisabled, ResolvedReadOnly);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private void HandleFocus(FocusEventArgs e)
    {
        if (ResolvedDisabled)
            return;

        FieldContext?.SetFocused(true);
    }

    private async Task HandleBlurAsync(FocusEventArgs e)
    {
        if (ResolvedDisabled)
            return;

        FieldContext?.SetTouched(true);
        FieldContext?.SetFocused(false);

        if (FieldContext?.ValidationMode == ValidationMode.OnBlur)
        {
            var validation = GroupContext?.Validation ?? FieldContext?.Validation;
            var valueToValidate = GroupContext is not null ? (object?)GroupContext.CheckedValue : CurrentChecked;
            await (validation?.CommitAsync(valueToValidate) ?? Task.CompletedTask);
        }
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (ResolvedDisabled || ResolvedReadOnly)
            return;

        if (e.Key == "Enter")
        {
            await EventUtilities.InvokeOnKeyDownAsync(AdditionalAttributes, e);
            return;
        }

        if (e.Key == " ")
        {
            await SelectRadioAsync();
            await EventUtilities.InvokeOnKeyDownAsync(AdditionalAttributes, e);
            return;
        }

        if (!IsInGroup || !GroupContext!.GroupElement.HasValue || !Element.HasValue)
        {
            await EventUtilities.InvokeOnKeyDownAsync(AdditionalAttributes, e);
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            switch (e.Key)
            {
                case "ArrowUp":
                case "ArrowLeft":
                    await module.InvokeVoidAsync("navigateToPrevious", GroupContext.GroupElement.Value, Element.Value);
                    break;
                case "ArrowDown":
                case "ArrowRight":
                    await module.InvokeVoidAsync("navigateToNext", GroupContext.GroupElement.Value, Element.Value);
                    break;
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }

        await EventUtilities.InvokeOnKeyDownAsync(AdditionalAttributes, e);
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (ResolvedDisabled || ResolvedReadOnly)
            return;

        await SelectRadioAsync();
        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }

    private async Task HandleInputChangeAsync(ChangeEventArgs e)
    {
        if (ResolvedDisabled || ResolvedReadOnly)
            return;

        await SelectRadioAsync();
    }

    private async Task HandleInputFocusAsync(FocusEventArgs e)
    {
        await FocusAsync();
    }

    private async Task SelectRadioAsync()
    {
        if (CurrentChecked)
            return;

        if (GroupContext is not null)
        {
            await GroupContext.SetCheckedValueAsync(Value);
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
