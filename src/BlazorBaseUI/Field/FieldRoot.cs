using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Form;
using BlazorBaseUI.Utilities.LabelableProvider;
using FormValidationMode = BlazorBaseUI.Form.ValidationMode;

namespace BlazorBaseUI.Field;

public sealed class FieldRoot : ComponentBase, IDisposable
{
    private const string DefaultTag = "div";

    private readonly HashSet<IFieldStateSubscriber> subscribers = [];

    private bool touched;
    private bool dirty;
    private bool filled;
    private bool focused;
    private FieldValidityData validityData = FieldValidityData.Default;
    private FieldValidation validation = null!;
    private FieldRootContext context = null!;
    private string fieldId = null!;
    private ElementReference element;
    private EditContext? previousEditContext;
    private Func<ValueTask>? focusHandler;

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    [CascadingParameter]
    private FormContext? FormContext { get; set; }

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

    [DisallowNull]
    public ElementReference? Element => element;

    private ValidationMode ResolvedValidationMode =>
        ValidationMode ?? FormContext?.ValidationMode ?? FormValidationMode.OnSubmit;

    private bool ResolvedDisabled => Disabled;

    private bool ResolvedTouched => TouchedState ?? touched;

    private bool ResolvedDirty => DirtyState ?? dirty;

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

    private FieldRootState CreateState() => new(
        Disabled: ResolvedDisabled,
        Valid: ComputeValid(),
        Touched: ResolvedTouched,
        Dirty: ResolvedDirty,
        Filled: filled,
        Focused: focused);

    private bool ShouldValidateOnChange() =>
        ResolvedValidationMode == FormValidationMode.OnChange ||
        (ResolvedValidationMode == FormValidationMode.OnSubmit &&
         FormContext?.GetSubmitAttempted() == true);

    protected override void OnInitialized()
    {
        fieldId = Guid.NewGuid().ToIdString();

        validation = new FieldValidation(
            getValidityData: () => validityData,
            setValidityData: data => validityData = data,
            validate: Validate,
            getInvalid: () => Invalid ?? false,
            debounceTime: ValidationDebounceTime,
            requestStateChange: NotifyStateChanged);

        context = CreateContext();
        RegisterWithForm();
    }

    protected override void OnParametersSet()
    {
        if (EditContext != previousEditContext)
        {
            DetachValidationStateChangedHandler();
            previousEditContext = EditContext;
            AttachValidationStateChangedHandler();
        }

        context = CreateContext();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = CreateState();
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        var attributes = BuildAttributes(state);
        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        builder.OpenComponent<LabelableProvider>(0);
        builder.AddComponentParameter(1, "InitialControlId", fieldId);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(labelableBuilder =>
        {
            labelableBuilder.OpenComponent<CascadingValue<FieldRootContext>>(3);
            labelableBuilder.AddComponentParameter(4, "Value", context);
            labelableBuilder.AddComponentParameter(5, "IsFixed", false);
            labelableBuilder.AddComponentParameter(6, "ChildContent", (RenderFragment)(contextBuilder =>
            {
                if (RenderAs is not null)
                {
                    contextBuilder.OpenComponent(7, RenderAs);
                    contextBuilder.AddMultipleAttributes(8, attributes);
                    contextBuilder.AddComponentParameter(9, "ChildContent", ChildContent);
                    contextBuilder.CloseComponent();
                }
                else
                {
                    var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
                    contextBuilder.OpenElement(10, tag);
                    contextBuilder.AddMultipleAttributes(11, attributes);
                    contextBuilder.AddElementReferenceCapture(12, e => element = e);
                    contextBuilder.AddContent(13, ChildContent);
                    contextBuilder.CloseElement();
                }
            }));
            labelableBuilder.CloseComponent();
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

    private FieldRootContext CreateContext() => new(
        Invalid: Invalid,
        Name: Name,
        ValidityData: validityData,
        SetValidityData: SetValidityData,
        Disabled: ResolvedDisabled,
        Touched: ResolvedTouched,
        SetTouched: SetTouched,
        Dirty: ResolvedDirty,
        SetDirty: SetDirty,
        Filled: filled,
        SetFilled: SetFilled,
        Focused: focused,
        SetFocused: SetFocused,
        ValidationMode: ResolvedValidationMode,
        ValidationDebounceTime: ValidationDebounceTime,
        ShouldValidateOnChangeFunc: ShouldValidateOnChange,
        RegisterFocusHandlerFunc: RegisterFocusHandler,
        SubscribeFunc: Subscribe,
        UnsubscribeFunc: Unsubscribe,
        State: CreateState(),
        Validation: validation);

    private Dictionary<string, object> BuildAttributes(FieldRootState state)
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

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
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

    private void NotifyStateChanged()
    {
        context = CreateContext();
        _ = InvokeAsync(StateHasChanged);

        foreach (var subscriber in subscribers)
        {
            subscriber.NotifyStateChanged();
        }
    }

    private void SetValidityData(FieldValidityData data)
    {
        validityData = data;
        context = CreateContext();
    }

    private void SetTouched(bool value)
    {
        if (TouchedState.HasValue) return;
        if (touched == value) return;
        touched = value;
        NotifyStateChanged();
    }

    private void SetDirty(bool value)
    {
        if (DirtyState.HasValue) return;
        if (dirty == value) return;
        dirty = value;
        NotifyStateChanged();
    }

    private void SetFilled(bool value)
    {
        if (filled == value) return;
        filled = value;
        NotifyStateChanged();
    }

    private void SetFocused(bool value)
    {
        if (focused == value) return;
        focused = value;
        NotifyStateChanged();
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
        NotifyStateChanged();
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