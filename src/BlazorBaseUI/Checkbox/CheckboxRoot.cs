using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorBaseUI.Field;
using BlazorBaseUI.Form;
using BlazorBaseUI.Utilities.LabelableProvider;
using BlazorBaseUI.CheckboxGroup;

namespace BlazorBaseUI.Checkbox;

public sealed class CheckboxRoot : ComponentBase, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "span";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-checkbox.js";
    private const string ParentCheckboxAttribute = "data-parent";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private bool isChecked;
    private bool previousChecked;
    private bool previousDisabled;
    private bool previousReadOnly;
    private bool previousIndeterminate;
    private string? defaultId;
    private string resolvedControlId = null!;
    private string checkboxId = null!;
    private string inputId = null!;
    private ElementReference inputElement;

    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter] private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter] private FieldItemContext? FieldItemContext { get; set; }

    [CascadingParameter] private FormContext? FormContext { get; set; }

    [CascadingParameter] private LabelableContext? LabelableContext { get; set; }

    [CascadingParameter] private CheckboxGroupContext? GroupContext { get; set; }

    [Parameter] public bool? Checked { get; set; }

    [Parameter] public bool DefaultChecked { get; set; }

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public bool ReadOnly { get; set; }

    [Parameter] public bool Required { get; set; }

    [Parameter] public bool Indeterminate { get; set; }

    [Parameter] public bool Parent { get; set; }

    [Parameter] public string? Name { get; set; }

    [Parameter] public string? Value { get; set; }

    [Parameter] public string? UncheckedValue { get; set; }

    [Parameter] public EventCallback<bool> CheckedChanged { get; set; }

    [Parameter] public EventCallback<CheckboxCheckedChangeEventArgs> OnCheckedChange { get; set; }

    [Parameter] public string? As { get; set; }

    [Parameter] public Type? RenderAs { get; set; }

    [Parameter] public Func<CheckboxRootState, string>? ClassValue { get; set; }

    [Parameter] public Func<CheckboxRootState, string>? StyleValue { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull] public ElementReference? Element { get; private set; }

    private bool IsControlled => Checked.HasValue;

    private bool IsGroupedWithParent => GroupContext?.Parent is not null && GroupContext.AllValues is not null;

    private bool CurrentChecked
    {
        get
        {
            if (IsGroupedWithParent && Parent)
                return GroupContext!.Parent!.Checked;

            if (GroupContext is not null && !Parent && ResolvedValue is not null)
                return GroupContext.Value?.Contains(ResolvedValue) ?? false;

            return IsControlled ? Checked!.Value : isChecked;
        }
    }

    private bool CurrentIndeterminate
    {
        get
        {
            if (IsGroupedWithParent && Parent)
                return GroupContext!.Parent!.Indeterminate;

            return Indeterminate;
        }
    }

    private bool ResolvedDisabled =>
        Disabled ||
        (FieldContext?.Disabled ?? false) ||
        (FieldItemContext?.Disabled ?? false) ||
        (GroupContext?.Disabled ?? false);

    private string? ResolvedName => Name ?? FieldContext?.Name;

    private string? ResolvedValue => Value ?? ResolvedName;

    private string ResolvedControlId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    private FieldRootState FieldState => FieldContext?.State ?? FieldRootState.Default;

    private CheckboxRootState State => CheckboxRootState.FromFieldState(
        FieldState,
        CurrentChecked,
        ResolvedDisabled,
        ReadOnly,
        Required,
        CurrentIndeterminate);

    public CheckboxRoot()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        checkboxId = Guid.NewGuid().ToIdString();
        resolvedControlId = ResolvedControlId;
        LabelableContext?.SetControlId(resolvedControlId);

        if (IsGroupedWithParent)
        {
            inputId = Parent
                ? GroupContext!.Parent!.Id ?? Guid.NewGuid().ToIdString()
                : $"{GroupContext!.Parent!.Id}-{ResolvedValue}";
        }
        else
        {
            inputId = resolvedControlId;
        }

        if (!IsControlled)
        {
            if (GroupContext is not null && !Parent && ResolvedValue is not null)
            {
                isChecked = GroupContext.DefaultValue?.Contains(ResolvedValue) ?? false;
            }
            else
            {
                isChecked = DefaultChecked;
            }
        }

        if (GroupContext is null)
        {
            var initialValue = CurrentChecked;
            FieldContext?.Validation.SetInitialValue(initialValue);
            FieldContext?.SetFilled(initialValue);
        }

        FieldContext?.RegisterFocusHandlerFunc(FocusAsync);
        FieldContext?.SubscribeFunc(this);
        GroupContext?.RegisterControlRef(this);

        if (IsGroupedWithParent && !Parent && ResolvedValue is not null)
        {
            GroupContext!.Parent!.SetDisabledState(ResolvedValue, ResolvedDisabled);
        }

        previousChecked = CurrentChecked;
        previousDisabled = ResolvedDisabled;
        previousReadOnly = ReadOnly;
        previousIndeterminate = CurrentIndeterminate;
    }

    protected override async Task OnParametersSetAsync()
    {
        var newResolvedId = ResolvedControlId;
        if (newResolvedId != resolvedControlId)
        {
            resolvedControlId = newResolvedId;
            if (!IsGroupedWithParent)
            {
                inputId = resolvedControlId;
            }
            LabelableContext?.SetControlId(resolvedControlId);
        }

        if (IsGroupedWithParent && !Parent && ResolvedValue is not null)
        {
            GroupContext!.Parent!.SetDisabledState(ResolvedValue, ResolvedDisabled);
        }

        if (hasRendered)
        {
            if (CurrentChecked != previousChecked)
            {
                previousChecked = CurrentChecked;
                await HandleCheckedChangedAsync();
            }

            if (ResolvedDisabled != previousDisabled ||
                ReadOnly != previousReadOnly ||
                CurrentIndeterminate != previousIndeterminate)
            {
                previousDisabled = ResolvedDisabled;
                previousReadOnly = ReadOnly;
                previousIndeterminate = CurrentIndeterminate;
                await UpdateJsStateAsync();
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = CreateContext(State);

        builder.OpenComponent<CascadingValue<CheckboxRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(contextBuilder =>
        {
            RenderCheckbox(contextBuilder, State);
            RenderHiddenInput(contextBuilder);
            RenderCheckboxInput(contextBuilder);
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

    private void RenderCheckbox(RenderTreeBuilder builder, CheckboxRootState state)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(State));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(State));
        var attributes = BuildCheckboxAttributes(state);

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
        if (CurrentChecked || GroupContext is not null || ResolvedName is null || Parent || UncheckedValue is null)
            return;

        builder.OpenElement(7, "input");
        builder.AddAttribute(8, "type", "hidden");
        builder.AddAttribute(9, "name", ResolvedName);
        builder.AddAttribute(10, "Value", UncheckedValue);
        builder.CloseElement();
    }

    private void RenderCheckboxInput(RenderTreeBuilder builder)
    {
        builder.OpenElement(11, "input");
        builder.AddAttribute(12, "type", "checkbox");
        builder.AddAttribute(13, "id", inputId);
        builder.AddAttribute(14, "checked", CurrentChecked);
        builder.AddAttribute(15, "Disabled", ResolvedDisabled);
        builder.AddAttribute(16, "required", Required);
        builder.AddAttribute(17, "aria-hidden", true);
        builder.AddAttribute(18, "tabindex", -1);
        builder.AddAttribute(19, "style",
            "position:absolute;pointer-events:none;opacity:0;margin:0;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0;");

        if (!Parent && ResolvedName is not null)
            builder.AddAttribute(20, "name", ResolvedName);

        if (ResolvedValue is not null)
        {
            var inputValue = ResolvedValue;
            builder.AddAttribute(21, "Value", inputValue);
        }

        builder.AddAttribute(22, "onchange",
            EventCallback.Factory.Create<ChangeEventArgs>(this, HandleInputChangeAsync));
        builder.AddAttribute(23, "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, HandleInputFocusAsync));
        builder.AddElementReferenceCapture(24, e => inputElement = e);
        builder.CloseElement();
    }

    private Dictionary<string, object> BuildCheckboxAttributes(CheckboxRootState state)
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

        attributes["id"] = checkboxId;
        attributes["role"] = "checkbox";
        attributes["aria-checked"] = CurrentIndeterminate ? "mixed" : CurrentChecked;
        attributes["tabindex"] = ResolvedDisabled ? -1 : 0;

        if (ReadOnly)
            attributes["aria-readonly"] = true;

        if (Required)
            attributes["aria-required"] = true;

        if (LabelableContext?.LabelId is not null)
            attributes["aria-labelledby"] = LabelableContext.LabelId;

        var describedBy = LabelableContext?.GetAriaDescribedBy();
        if (describedBy is not null)
            attributes["aria-describedby"] = describedBy;

        if (state.Valid == false)
            attributes["aria-invalid"] = true;

        if (Parent)
            attributes[ParentCheckboxAttribute] = string.Empty;

        if (IsGroupedWithParent && Parent && GroupContext!.Parent!.AriaControls is not null)
            attributes["aria-controls"] = GroupContext.Parent.AriaControls;

        attributes["onfocus"] = EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocus);
        attributes["onblur"] = EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlurAsync);

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }

    private CheckboxRootContext CreateContext(CheckboxRootState state) => new(
        Checked: CurrentChecked,
        Disabled: ResolvedDisabled,
        ReadOnly: ReadOnly,
        Required: Required,
        Indeterminate: CurrentIndeterminate,
        State: state);

    private async Task InitializeJsAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("initialize", Element, inputElement, ResolvedDisabled, ReadOnly,
                CurrentIndeterminate);
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
            await module.InvokeVoidAsync("updateState", Element, inputElement, ResolvedDisabled, ReadOnly,
                CurrentIndeterminate);
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

        var validation = GroupContext?.Validation ?? FieldContext?.Validation;
        if (FieldContext?.ValidationMode == ValidationMode.OnBlur)
        {
            if (GroupContext is not null)
            {
                await (validation?.CommitAsync(GroupContext.Value) ?? Task.CompletedTask);
            }
            else
            {
                await (validation?.CommitAsync(CurrentChecked) ?? Task.CompletedTask);
            }
        }
    }

    private async Task HandleInputChangeAsync(ChangeEventArgs e)
    {
        if (ReadOnly || ResolvedDisabled)
            return;

        var nextChecked = e.Value is bool b ? b : bool.TryParse(e.Value?.ToString(), out var parsed) && parsed;

        if (nextChecked == CurrentChecked && !CurrentIndeterminate)
            return;

        if (IsGroupedWithParent && Parent)
        {
            GroupContext!.Parent!.OnCheckedChange(nextChecked);
            return;
        }

        if (GroupContext is not null && !Parent && ResolvedValue is not null)
        {
            var currentValues = GroupContext.Value?.ToList() ?? new List<string>();
            if (nextChecked)
            {
                if (!currentValues.Contains(ResolvedValue))
                    currentValues.Add(ResolvedValue);
            }
            else
            {
                currentValues.Remove(ResolvedValue);
            }

            GroupContext.SetValue(currentValues.ToArray());
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
        var eventArgs = new CheckboxCheckedChangeEventArgs(value);

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

        await HandleCheckedChangedAsync();
        StateHasChanged();
    }

    private async Task ResetInputCheckedAsync()
    {
        if (!hasRendered)
            return;

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("setInputChecked", inputElement, CurrentChecked);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task HandleCheckedChangedAsync()
    {
        if (GroupContext is not null && !Parent)
            return;

        FormContext?.ClearErrors(ResolvedName);

        var initialValue = FieldContext?.ValidityData.InitialValue;
        var isDirty = initialValue is bool initial ? CurrentChecked != initial : CurrentChecked;

        FieldContext?.SetDirty(isDirty);
        FieldContext?.SetFilled(CurrentChecked);

        var validation = GroupContext?.Validation ?? FieldContext?.Validation;
        if (FieldContext?.ShouldValidateOnChangeFunc() == true)
        {
            await (validation?.CommitAsync(CurrentChecked) ?? Task.CompletedTask);
        }
        else
        {
            await (validation?.CommitAsync(CurrentChecked, revalidateOnly: true) ?? Task.CompletedTask);
        }
    }

    private async ValueTask FocusAsync()
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
}