using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using BlazorBaseUI.Field;
using BlazorBaseUI.Form;
using BlazorBaseUI.Utilities.LabelableProvider;
using BlazorBaseUI.CheckboxGroup;

namespace BlazorBaseUI.Checkbox;

public sealed class CheckboxRoot : ComponentBase, IReferencableComponent, IFieldStateSubscriber, IAsyncDisposable
{
    private const string DefaultTag = "span";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-checkbox.js";
    private const string ParentCheckboxAttribute = "data-parent";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private bool isChecked;
    private bool isComponentRenderAs;
    private string? defaultId;
    private string resolvedControlId = null!;
    private string checkboxId = null!;
    private string inputId = null!;
    private ElementReference inputElement;
    private CheckboxRootState state = CheckboxRootState.Default;
    private CheckboxRootContext context = CheckboxRootContext.Default;
    private bool previousChecked;
    private bool previousDisabled;
    private bool previousReadOnly;
    private bool previousRequired;
    private bool previousIndeterminate;
    private bool? previousValid;
    private bool previousTouched;
    private bool previousDirty;
    private bool previousFilled;
    private bool previousFocused;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private FieldItemContext? FieldItemContext { get; set; }

    [CascadingParameter]
    private FormContext? FormContext { get; set; }

    [CascadingParameter]
    private LabelableContext? LabelableContext { get; set; }

    [CascadingParameter]
    private CheckboxGroupContext? GroupContext { get; set; }

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
    public bool Indeterminate { get; set; }

    [Parameter]
    public bool Parent { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public string? Value { get; set; }

    [Parameter]
    public string? UncheckedValue { get; set; }

    [Parameter]
    public EventCallback<bool> CheckedChanged { get; set; }

    [Parameter]
    public EventCallback<CheckboxCheckedChangeEventArgs> OnCheckedChange { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<CheckboxRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<CheckboxRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

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
        previousRequired = Required;
        previousIndeterminate = CurrentIndeterminate;
        previousValid = FieldState.Valid;
        previousTouched = FieldState.Touched;
        previousDirty = FieldState.Dirty;
        previousFilled = FieldState.Filled;
        previousFocused = FieldState.Focused;

        state = CheckboxRootState.FromFieldState(
            FieldState,
            CurrentChecked,
            ResolvedDisabled,
            ReadOnly,
            Required,
            CurrentIndeterminate);

        context = new CheckboxRootContext(
            Checked: CurrentChecked,
            Disabled: ResolvedDisabled,
            ReadOnly: ReadOnly,
            Required: Required,
            Indeterminate: CurrentIndeterminate,
            State: state);
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

        var currentChecked = CurrentChecked;
        var currentDisabled = ResolvedDisabled;
        var currentIndeterminate = CurrentIndeterminate;
        var fieldState = FieldState;

        var stateChanged = previousChecked != currentChecked ||
                           previousDisabled != currentDisabled ||
                           previousReadOnly != ReadOnly ||
                           previousRequired != Required ||
                           previousIndeterminate != currentIndeterminate ||
                           previousValid != fieldState.Valid ||
                           previousTouched != fieldState.Touched ||
                           previousDirty != fieldState.Dirty ||
                           previousFilled != fieldState.Filled ||
                           previousFocused != fieldState.Focused;

        if (stateChanged)
        {
            state = CheckboxRootState.FromFieldState(
                fieldState,
                currentChecked,
                currentDisabled,
                ReadOnly,
                Required,
                currentIndeterminate);

            context = new CheckboxRootContext(
                Checked: currentChecked,
                Disabled: currentDisabled,
                ReadOnly: ReadOnly,
                Required: Required,
                Indeterminate: currentIndeterminate,
                State: state);
        }

        if (hasRendered)
        {
            if (currentChecked != previousChecked)
            {
                _ = HandleCheckedChangedAsync();
            }

            if (currentDisabled != previousDisabled ||
                ReadOnly != previousReadOnly ||
                currentIndeterminate != previousIndeterminate)
            {
                _ = UpdateJsStateAsync();
            }
        }

        previousChecked = currentChecked;
        previousDisabled = currentDisabled;
        previousReadOnly = ReadOnly;
        previousRequired = Required;
        previousIndeterminate = currentIndeterminate;
        previousValid = fieldState.Valid;
        previousTouched = fieldState.Touched;
        previousDirty = fieldState.Dirty;
        previousFilled = fieldState.Filled;
        previousFocused = fieldState.Focused;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<CheckboxRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)RenderContent);
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
        builder.AddAttribute(2, "id", checkboxId);
        builder.AddAttribute(3, "role", "checkbox");
        builder.AddAttribute(4, "aria-checked", CurrentIndeterminate ? "mixed" : CurrentChecked ? "true" : "false");
        builder.AddAttribute(5, "tabindex", ResolvedDisabled ? -1 : 0);

        if (ReadOnly)
        {
            builder.AddAttribute(6, "aria-readonly", "true");
        }

        if (Required)
        {
            builder.AddAttribute(7, "aria-required", "true");
        }

        if (!string.IsNullOrEmpty(LabelableContext?.LabelId))
        {
            builder.AddAttribute(8, "aria-labelledby", LabelableContext.LabelId);
        }

        var describedBy = LabelableContext?.GetAriaDescribedBy();
        if (!string.IsNullOrEmpty(describedBy))
        {
            builder.AddAttribute(9, "aria-describedby", describedBy);
        }

        if (state.Valid == false)
        {
            builder.AddAttribute(10, "aria-invalid", "true");
        }

        if (Parent)
        {
            builder.AddAttribute(11, ParentCheckboxAttribute, string.Empty);
        }

        if (IsGroupedWithParent && Parent && !string.IsNullOrEmpty(GroupContext?.Parent?.AriaControls))
        {
            builder.AddAttribute(12, "aria-controls", GroupContext.Parent.AriaControls);
        }

        builder.AddAttribute(13, "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocus));
        builder.AddAttribute(14, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlurAsync));

        if (state.Indeterminate)
        {
            builder.AddAttribute(15, "data-indeterminate", string.Empty);
        }
        else if (state.Checked)
        {
            builder.AddAttribute(16, "data-checked", string.Empty);
        }
        else
        {
            builder.AddAttribute(17, "data-unchecked", string.Empty);
        }

        if (state.Disabled)
        {
            builder.AddAttribute(18, "data-disabled", string.Empty);
        }

        if (state.ReadOnly)
        {
            builder.AddAttribute(19, "data-readonly", string.Empty);
        }

        if (state.Required)
        {
            builder.AddAttribute(20, "data-required", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(21, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(22, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(23, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(24, "data-dirty", string.Empty);
        }

        if (state.Filled)
        {
            builder.AddAttribute(25, "data-filled", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(26, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(27, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(28, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(29, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(30, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(29, elementReference => Element = elementReference);
            builder.AddContent(30, ChildContent);
            builder.CloseElement();
        }

        if (!CurrentChecked && GroupContext is null && ResolvedName is not null && !Parent && UncheckedValue is not null)
        {
            builder.OpenElement(31, "input");
            builder.AddAttribute(32, "type", "hidden");
            builder.AddAttribute(33, "name", ResolvedName);
            builder.AddAttribute(34, "value", UncheckedValue);
            builder.CloseElement();
        }

        builder.OpenElement(35, "input");
        builder.AddAttribute(36, "type", "checkbox");
        builder.AddAttribute(37, "id", inputId);
        builder.AddAttribute(38, "checked", CurrentChecked);
        builder.AddAttribute(39, "disabled", ResolvedDisabled);
        builder.AddAttribute(40, "required", Required);
        builder.AddAttribute(41, "aria-hidden", "true");
        builder.AddAttribute(42, "tabindex", -1);
        builder.AddAttribute(43, "style", "position:absolute;pointer-events:none;opacity:0;margin:0;width:1px;height:1px;overflow:hidden;clip:rect(0,0,0,0);white-space:nowrap;border:0;");

        if (!Parent && ResolvedName is not null)
        {
            builder.AddAttribute(44, "name", ResolvedName);
        }

        if (ResolvedValue is not null)
        {
            builder.AddAttribute(45, "value", ResolvedValue);
        }

        builder.AddAttribute(46, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, HandleInputChangeAsync));
        builder.AddAttribute(47, "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, HandleInputFocusAsync));
        builder.AddElementReferenceCapture(48, e => inputElement = e);
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

    private async Task InitializeJsAsync()
    {
        if (!Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("initialize", Element.Value, inputElement, ResolvedDisabled, ReadOnly, CurrentIndeterminate);
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
            await module.InvokeVoidAsync("updateState", Element.Value, inputElement, ResolvedDisabled, ReadOnly, CurrentIndeterminate);
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

    private async Task HandleBlurAsync(FocusEventArgs e)
    {
        if (ResolvedDisabled)
        {
            return;
        }

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
        {
            return;
        }

        var nextChecked = e.Value is bool b ? b : bool.TryParse(e.Value?.ToString(), out var parsed) && parsed;

        if (nextChecked == CurrentChecked && !CurrentIndeterminate)
        {
            return;
        }

        if (IsGroupedWithParent && Parent)
        {
            GroupContext!.Parent!.OnCheckedChange(nextChecked);
            return;
        }

        if (GroupContext is not null && !Parent && ResolvedValue is not null)
        {
            var currentValues = GroupContext.Value?.ToList() ?? [];
            if (nextChecked)
            {
                if (!currentValues.Contains(ResolvedValue))
                {
                    currentValues.Add(ResolvedValue);
                }
            }
            else
            {
                currentValues.Remove(ResolvedValue);
            }

            GroupContext.SetValue([.. currentValues]);
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
            UpdateStateAndContext();
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

    private async Task HandleCheckedChangedAsync()
    {
        if (GroupContext is not null && !Parent)
        {
            return;
        }

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

    private void UpdateStateAndContext()
    {
        var currentChecked = CurrentChecked;
        var currentDisabled = ResolvedDisabled;
        var currentIndeterminate = CurrentIndeterminate;
        var fieldState = FieldState;

        state = CheckboxRootState.FromFieldState(
            fieldState,
            currentChecked,
            currentDisabled,
            ReadOnly,
            Required,
            currentIndeterminate);

        context = new CheckboxRootContext(
            Checked: currentChecked,
            Disabled: currentDisabled,
            ReadOnly: ReadOnly,
            Required: Required,
            Indeterminate: currentIndeterminate,
            State: state);

        previousChecked = currentChecked;
        previousDisabled = currentDisabled;
        previousIndeterminate = currentIndeterminate;
        previousReadOnly = ReadOnly;
        previousRequired = Required;
        previousValid = fieldState.Valid;
        previousTouched = fieldState.Touched;
        previousDirty = fieldState.Dirty;
        previousFilled = fieldState.Filled;
        previousFocused = fieldState.Focused;
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
