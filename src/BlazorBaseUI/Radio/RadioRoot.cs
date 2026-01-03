using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorBaseUI.Field;
using BlazorBaseUI.RadioGroup;
using BlazorBaseUI.Utilities.LabelableProvider;
using BlazorBaseUI.Form;

namespace BlazorBaseUI.Radio;

public sealed class RadioRoot<TValue> : ComponentBase, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "span";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-radio.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private bool previousDisabled;
    private bool previousReadOnly;
    private string? defaultId;
    private string resolvedControlId = null!;
    private string radioId = null!;
    private string inputId = null!;
    private ElementReference inputElement;

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

    [DisallowNull]
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

    private RadioRootState State => RadioRootState.FromFieldState(
        FieldState,
        CurrentChecked,
        ResolvedDisabled,
        ResolvedReadOnly,
        ResolvedRequired);

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
    }

    protected override async Task OnParametersSetAsync()
    {
        var newResolvedId = ResolvedControlId;
        if (newResolvedId != resolvedControlId)
        {
            resolvedControlId = newResolvedId;
            inputId = resolvedControlId;
            LabelableContext?.SetControlId(resolvedControlId);
        }

        if (hasRendered)
        {
            if (ResolvedDisabled != previousDisabled ||
                ResolvedReadOnly != previousReadOnly)
            {
                previousDisabled = ResolvedDisabled;
                previousReadOnly = ResolvedReadOnly;
                await UpdateJsStateAsync();
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = CreateContext(State);

        builder.OpenComponent<CascadingValue<RadioRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(contextBuilder =>
        {
            RenderRadio(contextBuilder, State);
            RenderHiddenInput(contextBuilder);
        }));
        builder.CloseComponent();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Element.HasValue)
        {
            hasRendered = true;
            GroupContext?.RegisterRadio(this, Element.Value, Value, () => ResolvedDisabled, FocusAsync);
            await InitializeJsAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        LabelableContext?.SetControlId(null);
        FieldContext?.UnsubscribeFunc(this);
        GroupContext?.UnregisterRadio(this);

        if (moduleTask.IsValueCreated && Element.HasValue)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("dispose", Element);
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

    internal async ValueTask FocusAsync()
    {
        if (!hasRendered)
            return;

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("focus", Element);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private void RenderRadio(RenderTreeBuilder builder, RadioRootState state)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(State));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(State));
        var attributes = BuildRadioAttributes(state);

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
        builder.AddElementReferenceCapture(5, e => Element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    private void RenderHiddenInput(RenderTreeBuilder builder)
    {
        builder.OpenElement(7, "input");
        builder.AddAttribute(8, "type", "radio");
        builder.AddAttribute(9, "id", inputId);
        builder.AddAttribute(10, "tabindex", -1);
        builder.AddAttribute(11, "aria-hidden", true);
        builder.AddAttribute(12, "style",
            "position:absolute;pointer-events:none;opacity:0;margin:0;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0;");

        if (CurrentChecked)
            builder.AddAttribute(13, "checked", true);

        if (ResolvedDisabled)
            builder.AddAttribute(14, "Disabled", true);

        if (ResolvedRequired)
            builder.AddAttribute(15, "required", true);

        if (ResolvedName is not null)
            builder.AddAttribute(16, "name", ResolvedName);

        if (Value is not null)
            builder.AddAttribute(17, "Value", Value.ToString());

        builder.AddAttribute(18, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, HandleInputChangeAsync));
        builder.AddAttribute(19, "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, HandleInputFocusAsync));
        builder.AddElementReferenceCapture(20, e => inputElement = e);
        builder.CloseElement();
    }

    private Dictionary<string, object> BuildRadioAttributes(RadioRootState state)
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

        attributes["id"] = radioId;
        attributes["role"] = "radio";
        attributes["aria-checked"] = CurrentChecked ? "true" : "false";

        if (IsInGroup)
        {
            var isActiveItem = CurrentChecked || (GroupContext!.CheckedValue is null && GroupContext.IsFirstEnabledRadio(this));
            attributes["tabindex"] = ResolvedDisabled ? -1 : (isActiveItem ? 0 : -1);
            attributes["data-radio-item"] = string.Empty;
        }
        else
        {
            attributes["tabindex"] = ResolvedDisabled ? -1 : 0;
        }

        if (ResolvedReadOnly)
            attributes["aria-readonly"] = true;

        if (ResolvedRequired)
            attributes["aria-required"] = true;

        if (LabelableContext?.LabelId is not null)
            attributes["aria-labelledby"] = LabelableContext.LabelId;

        var describedBy = LabelableContext?.GetAriaDescribedBy();
        if (describedBy is not null)
            attributes["aria-describedby"] = describedBy;

        if (state.Valid == false)
            attributes["aria-invalid"] = true;

        attributes["onkeydown"] = EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync);
        attributes["onclick"] = EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync);
        attributes["onfocus"] = EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocus);
        attributes["onblur"] = EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlurAsync);

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }

    private RadioRootContext CreateContext(RadioRootState state) => new(
        Checked: CurrentChecked,
        Disabled: ResolvedDisabled,
        ReadOnly: ResolvedReadOnly,
        Required: ResolvedRequired,
        State: state);

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
            return;

        if (e.Key == " ")
        {
            await SelectRadioAsync();
            return;
        }

        if (!IsInGroup)
            return;

        switch (e.Key)
        {
            case "ArrowUp":
            case "ArrowLeft":
                await GroupContext!.NavigateToPreviousAsync(this);
                break;
            case "ArrowDown":
            case "ArrowRight":
                await GroupContext!.NavigateToNextAsync(this);
                break;
        }
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (ResolvedDisabled || ResolvedReadOnly)
            return;

        await SelectRadioAsync();
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
}
