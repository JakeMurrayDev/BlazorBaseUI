using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
using BlazorBaseUI.Fieldset;
using BlazorBaseUI.Form;
using BlazorBaseUI.Utilities.LabelableProvider;
using FormValidationMode = BlazorBaseUI.Form.ValidationMode;

namespace BlazorBaseUI.Field;

public sealed class FieldRootActions
{
    private readonly Func<Task> validateAsync;

    internal FieldRootActions(Func<Task> validateAsync)
    {
        this.validateAsync = validateAsync;
    }

    public Task ValidateAsync() => validateAsync();
}

public sealed class FieldRoot : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "div";

    private readonly HashSet<IFieldStateSubscriber> subscribers = [];

    private Func<Task> cachedLabelableStateChangedCallback = default!;
    private Func<Task> cachedNotifyStateChangedCallback = default!;

    private string? controlId;
    private string? labelId;
    private List<string> messageIds = [];
    private bool labelableNotifyPending;
    private bool touched;
    private bool dirty;
    private bool filled;
    private bool focused;
    private bool notifyPending;
    private bool isComponentRenderAs;
    private bool markedDirty;
    private FieldValidityData validityData = FieldValidityData.Default;
    private FieldValidation validation = null!;
    private FieldRootContext context = null!;
    private LabelableContext labelableContext = null!;
    private FieldRootState state = FieldRootState.Default;
    private string fieldId = null!;
    private EditContext? previousEditContext;
    private Func<ValueTask>? focusHandler;
    private bool previousDisabled;
    private bool? previousValid;
    private bool previousTouched;
    private bool previousDirty;
    private bool previousFilled;
    private bool previousFocused;
    private FieldRootActions? actions;

    private ValidationMode ResolvedValidationMode =>
        ValidationMode ?? FormContext?.ValidationMode ?? FormValidationMode.OnSubmit;

    private bool ResolvedDisabled => FieldsetContext?.Disabled == true || Disabled;

    private bool ResolvedTouched => TouchedState ?? touched;

    private bool ResolvedDirty => DirtyState ?? dirty;

    [Inject]
    private ILogger<FieldRoot> Logger { get; set; } = default!;

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    [CascadingParameter]
    private FormContext? FormContext { get; set; }

    [CascadingParameter]
    private FieldsetRootContext? FieldsetContext { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public Func<object?, Task<string[]?>>? Validate { get; set; }

    [Parameter]
    public ValidationMode? ValidationMode { get; set; }

    [Parameter]
    public int ValidationDebounceTime { get; set; }

    [Parameter]
    public bool? Invalid { get; set; }

    [Parameter]
    public bool? DirtyState { get; set; }

    [Parameter]
    public bool? TouchedState { get; set; }

    [Parameter]
    public Action<FieldRootActions>? ActionsRef { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<FieldRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<FieldRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        fieldId = Guid.NewGuid().ToIdString();
        controlId = fieldId;

        cachedLabelableStateChangedCallback = () =>
        {
            try
            {
                labelableNotifyPending = false;
                labelableContext = CreateLabelableContext();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating labelable state in {Component}", nameof(FieldRoot));
            }
            return Task.CompletedTask;
        };

        cachedNotifyStateChangedCallback = () =>
        {
            try
            {
                ExecuteNotifyStateChanged();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error notifying state changed in {Component}", nameof(FieldRoot));
            }
            return Task.CompletedTask;
        };

        validation = new FieldValidation(
            getValidityData: () => validityData,
            setValidityData: data => validityData = data,
            validate: Validate,
            getInvalid: () => Invalid ?? false,
            getMarkedDirty: () => markedDirty,
            debounceTime: ValidationDebounceTime,
            requestStateChange: ScheduleNotifyStateChanged,
            logError: (ex, message) => Logger.LogError(ex, "{Message} in {Component}", message, nameof(FieldRoot)));

        state = new FieldRootState(
            Disabled: ResolvedDisabled,
            Valid: ComputeValid(),
            Touched: ResolvedTouched,
            Dirty: ResolvedDirty,
            Filled: filled,
            Focused: focused);

        context = new FieldRootContext(
            setValidityData: SetValidityData,
            setTouched: SetTouched,
            setDirty: SetDirty,
            setFilled: SetFilled,
            setFocused: SetFocused,
            shouldValidateOnChange: ShouldValidateOnChange,
            registerFocusHandler: RegisterFocusHandler,
            subscribe: Subscribe,
            unsubscribe: Unsubscribe,
            validation: validation);

        labelableContext = CreateLabelableContext();

        actions = new FieldRootActions(ImperativeValidateAsync);
        ActionsRef?.Invoke(actions);

        UpdateContext();
        RegisterWithForm();

        previousDisabled = ResolvedDisabled;
        previousValid = ComputeValid();
        previousTouched = ResolvedTouched;
        previousDirty = ResolvedDirty;
        previousFilled = filled;
        previousFocused = focused;
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (EditContext != previousEditContext)
        {
            DetachValidationStateChangedHandler();
            previousEditContext = EditContext;
            AttachValidationStateChangedHandler();
        }

        var currentDisabled = ResolvedDisabled;
        var currentValid = ComputeValid();
        var currentTouched = ResolvedTouched;
        var currentDirty = ResolvedDirty;

        var stateChanged = previousDisabled != currentDisabled ||
                           previousValid != currentValid ||
                           previousTouched != currentTouched ||
                           previousDirty != currentDirty ||
                           previousFilled != filled ||
                           previousFocused != focused;

        if (stateChanged)
        {
            state = new FieldRootState(
                Disabled: currentDisabled,
                Valid: currentValid,
                Touched: currentTouched,
                Dirty: currentDirty,
                Filled: filled,
                Focused: focused);

            previousDisabled = currentDisabled;
            previousValid = currentValid;
            previousTouched = currentTouched;
            previousDirty = currentDirty;
            previousFilled = filled;
            previousFocused = focused;
        }

        UpdateContext();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<LabelableContext>>(0);
        builder.AddComponentParameter(1, "Value", labelableContext);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(builder2 =>
        {
            builder2.OpenComponent<CascadingValue<FieldRootContext>>(0);
            builder2.AddComponentParameter(1, "Value", context);
            builder2.AddComponentParameter(2, "ChildContent", (RenderFragment)RenderContent);
            builder2.CloseComponent();
        }));
        builder.CloseComponent();
    }

    public void Dispose()
    {
        DetachValidationStateChangedHandler();
        FormContext?.FieldRegistry.Unregister(fieldId);
        validation.Dispose();
        subscribers.Clear();
    }

    private bool? ComputeValid()
    {
        if (Invalid == true)
            return false;

        if (EditContext is not null && Name is not null)
        {
            var fieldIdentifier = EditContext.Field(Name);
            if (EditContext.GetValidationMessages(fieldIdentifier).Any())
                return false;
        }

        if (FormContext?.HasError(Name) == true)
            return false;

        if (validityData.State.Valid == false)
            return false;

        return validityData.State.Valid;
    }

    private bool ShouldValidateOnChange() =>
        ResolvedValidationMode == FormValidationMode.OnChange ||
        (ResolvedValidationMode == FormValidationMode.OnSubmit &&
         FormContext?.GetSubmitAttempted() == true);

    private void RenderContent(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);

            if (state.Disabled)
            {
                builder.AddAttribute(2, "data-disabled", string.Empty);
            }

            if (state.Valid == true)
            {
                builder.AddAttribute(3, "data-valid", string.Empty);
            }
            else if (state.Valid == false)
            {
                builder.AddAttribute(4, "data-invalid", string.Empty);
            }

            if (state.Touched)
            {
                builder.AddAttribute(5, "data-touched", string.Empty);
            }

            if (state.Dirty)
            {
                builder.AddAttribute(6, "data-dirty", string.Empty);
            }

            if (state.Filled)
            {
                builder.AddAttribute(7, "data-filled", string.Empty);
            }

            if (state.Focused)
            {
                builder.AddAttribute(8, "data-focused", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(9, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(10, "style", resolvedStyle);
            }

            builder.AddAttribute(11, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(12, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);

            if (state.Disabled)
            {
                builder.AddAttribute(2, "data-disabled", string.Empty);
            }

            if (state.Valid == true)
            {
                builder.AddAttribute(3, "data-valid", string.Empty);
            }
            else if (state.Valid == false)
            {
                builder.AddAttribute(4, "data-invalid", string.Empty);
            }

            if (state.Touched)
            {
                builder.AddAttribute(5, "data-touched", string.Empty);
            }

            if (state.Dirty)
            {
                builder.AddAttribute(6, "data-dirty", string.Empty);
            }

            if (state.Filled)
            {
                builder.AddAttribute(7, "data-filled", string.Empty);
            }

            if (state.Focused)
            {
                builder.AddAttribute(8, "data-focused", string.Empty);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                builder.AddAttribute(9, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                builder.AddAttribute(10, "style", resolvedStyle);
            }

            builder.AddElementReferenceCapture(11, elementReference => Element = elementReference);
            builder.AddContent(12, ChildContent);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    private LabelableContext CreateLabelableContext() => new(
        ControlId: controlId,
        SetControlId: SetControlId,
        LabelId: labelId,
        SetLabelId: SetLabelId,
        MessageIds: messageIds,
        UpdateMessageIds: UpdateMessageIds);

    private void ScheduleLabelableStateHasChanged()
    {
        if (labelableNotifyPending)
            return;

        labelableNotifyPending = true;
        _ = InvokeAsync(cachedLabelableStateChangedCallback);
    }

    private void SetControlId(string? id)
    {
        if (controlId == id) return;
        controlId = id;
        ScheduleLabelableStateHasChanged();
    }

    private void SetLabelId(string? id)
    {
        if (labelId == id) return;
        labelId = id;
        ScheduleLabelableStateHasChanged();
    }

    private void UpdateMessageIds(string id, bool add)
    {
        if (add)
        {
            if (messageIds.Contains(id)) return;
            messageIds = [.. messageIds, id];
        }
        else
        {
            if (!messageIds.Contains(id)) return;
            messageIds = messageIds.Where(m => m != id).ToList();
        }
        ScheduleLabelableStateHasChanged();
    }

    private void UpdateContext()
    {
        context.Update(
            invalid: Invalid,
            name: Name,
            validityData: validityData,
            disabled: ResolvedDisabled,
            touched: ResolvedTouched,
            dirty: ResolvedDirty,
            filled: filled,
            focused: focused,
            validationMode: ResolvedValidationMode,
            validationDebounceTime: ValidationDebounceTime,
            state: state);
    }

    private void RegisterWithForm()
    {
        FormContext?.FieldRegistry.Register(fieldId, new FieldRegistration(
            name: Name,
            getValue: () => validityData.Value,
            validateAsync: () => validation.CommitAsync(validityData.Value),
            getValidityData: () => validityData,
            focusAsync: () => focusHandler?.Invoke() ?? ValueTask.CompletedTask));
    }

    private void RegisterFocusHandler(Func<ValueTask> handler)
    {
        focusHandler = handler;
    }

    private void Subscribe(IFieldStateSubscriber subscriber)
    {
        subscribers.Add(subscriber);
    }

    private void Unsubscribe(IFieldStateSubscriber subscriber)
    {
        subscribers.Remove(subscriber);
    }

    private void ScheduleNotifyStateChanged()
    {
        if (notifyPending)
            return;

        notifyPending = true;
        _ = InvokeAsync(cachedNotifyStateChangedCallback);
    }

    private void ExecuteNotifyStateChanged()
    {
        notifyPending = false;

        var currentValid = ComputeValid();
        state = new FieldRootState(
            Disabled: ResolvedDisabled,
            Valid: currentValid,
            Touched: ResolvedTouched,
            Dirty: ResolvedDirty,
            Filled: filled,
            Focused: focused);

        previousDisabled = ResolvedDisabled;
        previousValid = currentValid;
        previousTouched = ResolvedTouched;
        previousDirty = ResolvedDirty;
        previousFilled = filled;
        previousFocused = focused;

        UpdateContext();
        StateHasChanged();

        foreach (var subscriber in subscribers)
        {
            subscriber.NotifyStateChanged();
        }
    }

    private void SetValidityData(FieldValidityData data)
    {
        validityData = data;
        UpdateContext();
    }

    private void SetTouched(bool value)
    {
        if (TouchedState.HasValue) return;
        if (touched == value) return;
        touched = value;
        ScheduleNotifyStateChanged();
    }

    private void SetDirty(bool value)
    {
        if (DirtyState.HasValue) return;
        if (value)
            markedDirty = true;
        if (dirty == value) return;
        dirty = value;
        ScheduleNotifyStateChanged();
    }

    private async Task ImperativeValidateAsync()
    {
        markedDirty = true;
        await validation.CommitAsync(validityData.Value);
    }

    private void SetFilled(bool value)
    {
        if (filled == value) return;
        filled = value;
        ScheduleNotifyStateChanged();
    }

    private void SetFocused(bool value)
    {
        if (focused == value) return;
        focused = value;
        ScheduleNotifyStateChanged();
    }

    private void AttachValidationStateChangedHandler()
    {
        if (EditContext is not null)
        {
            EditContext.OnValidationStateChanged += HandleValidationStateChanged;
        }
    }

    private void DetachValidationStateChangedHandler()
    {
        if (previousEditContext is not null)
        {
            previousEditContext.OnValidationStateChanged -= HandleValidationStateChanged;
        }
    }

    private void HandleValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        ScheduleNotifyStateChanged();
    }

    private sealed class FieldRegistration(
        string? name,
        Func<object?> getValue,
        Func<Task> validateAsync,
        Func<FieldValidityData> getValidityData,
        Func<ValueTask> focusAsync) : IFieldRegistration
    {
        public string? Name => name;
        public Func<object?> GetValue => getValue;
        public Func<Task> ValidateAsync => validateAsync;
        public FieldValidityData ValidityData => getValidityData();
        public Func<ValueTask> FocusAsync => focusAsync;
    }
}
