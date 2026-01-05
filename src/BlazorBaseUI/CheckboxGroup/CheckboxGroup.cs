using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Field;
using BlazorBaseUI.Form;
using BlazorBaseUI.Utilities.LabelableProvider;
using BlazorBaseUI.Checkbox;

namespace BlazorBaseUI.CheckboxGroup;

public sealed class CheckboxGroup : ComponentBase, IFieldStateSubscriber, IDisposable
{
    private const string DefaultTag = "div";

    private string? defaultId;
    private string groupId = null!;
    private string[]? internalValue;
    private string[]? previousValue;
    private CheckboxRoot? controlRef;
    private CheckboxGroupState state = CheckboxGroupState.Default;
    private CheckboxGroupContext? context;
    private bool previousDisabled;
    private bool? previousValid;
    private bool previousTouched;
    private bool previousDirty;
    private bool previousFilled;
    private bool previousFocused;

    [CascadingParameter]
    private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter]
    private FormContext? FormContext { get; set; }

    [CascadingParameter]
    private LabelableContext? LabelableContext { get; set; }

    [Parameter]
    public string[]? Value { get; set; }

    [Parameter]
    public string[]? DefaultValue { get; set; }

    [Parameter]
    public string[]? AllValues { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback<string[]> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<CheckboxGroupValueChangeEventArgs> OnValueChange { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<CheckboxGroupState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<CheckboxGroupState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool IsControlled => Value is not null;

    private string[]? CurrentValue => IsControlled ? Value : internalValue;

    private bool ResolvedDisabled => Disabled || (FieldContext?.Disabled ?? false);

    private string? ResolvedName => FieldContext?.Name;

    private FieldRootState FieldState => FieldContext?.State ?? FieldRootState.Default;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    protected override void OnInitialized()
    {
        groupId = ResolvedId;

        if (!IsControlled)
        {
            internalValue = DefaultValue ?? [];
        }

        var initialValue = CurrentValue ?? [];
        FieldContext?.Validation.SetInitialValue(initialValue);
        FieldContext?.SetFilled(initialValue.Length > 0);
        FieldContext?.SubscribeFunc(this);

        previousValue = CurrentValue;
        previousDisabled = ResolvedDisabled;
        previousValid = FieldState.Valid;
        previousTouched = FieldState.Touched;
        previousDirty = FieldState.Dirty;
        previousFilled = FieldState.Filled;
        previousFocused = FieldState.Focused;

        state = CheckboxGroupState.FromFieldState(FieldState, ResolvedDisabled);
        context = CreateContext();
    }

    protected override void OnParametersSet()
    {
        var currentDisabled = ResolvedDisabled;
        var fieldState = FieldState;

        var stateChanged = previousDisabled != currentDisabled ||
                           previousValid != fieldState.Valid ||
                           previousTouched != fieldState.Touched ||
                           previousDirty != fieldState.Dirty ||
                           previousFilled != fieldState.Filled ||
                           previousFocused != fieldState.Focused;

        if (stateChanged)
        {
            state = CheckboxGroupState.FromFieldState(fieldState, currentDisabled);
        }

        var valueChanged = !ArraysEqual(CurrentValue, previousValue);

        if (stateChanged || valueChanged)
        {
            context = CreateContext();
        }

        previousDisabled = currentDisabled;
        previousValid = fieldState.Valid;
        previousTouched = fieldState.Touched;
        previousDirty = fieldState.Dirty;
        previousFilled = fieldState.Filled;
        previousFocused = fieldState.Focused;

        if (valueChanged)
        {
            previousValue = CurrentValue;
            _ = HandleValueChangedAsync();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<CheckboxGroupContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)RenderContent);
        builder.CloseComponent();
    }

    private void RenderContent(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var isComponent = RenderAs is not null;

        if (isComponent)
        {
            if (!typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
            {
                throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
            }
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "id", groupId);
        builder.AddAttribute(3, "role", "group");

        if (LabelableContext?.LabelId is not null)
        {
            builder.AddAttribute(4, "aria-labelledby", LabelableContext.LabelId);
        }

        var describedBy = LabelableContext?.GetAriaDescribedBy();
        if (describedBy is not null)
        {
            builder.AddAttribute(5, "aria-describedby", describedBy);
        }

        if (state.Disabled)
        {
            builder.AddAttribute(6, "data-disabled", string.Empty);
        }

        if (state.Valid == true)
        {
            builder.AddAttribute(7, "data-valid", string.Empty);
        }
        else if (state.Valid == false)
        {
            builder.AddAttribute(8, "data-invalid", string.Empty);
        }

        if (state.Touched)
        {
            builder.AddAttribute(9, "data-touched", string.Empty);
        }

        if (state.Dirty)
        {
            builder.AddAttribute(10, "data-dirty", string.Empty);
        }

        if (state.Filled)
        {
            builder.AddAttribute(11, "data-filled", string.Empty);
        }

        if (state.Focused)
        {
            builder.AddAttribute(12, "data-focused", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(13, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(14, "style", resolvedStyle);
        }

        if (isComponent)
        {
            builder.AddAttribute(15, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(16, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(17, elementReference => Element = elementReference);
            builder.AddContent(18, ChildContent);
            builder.CloseElement();
        }
    }

    public void NotifyStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        FieldContext?.UnsubscribeFunc(this);
    }

    private CheckboxGroupContext CreateContext()
    {
        CheckboxGroupParent? parent = null;
        if (AllValues is not null)
        {
            parent = new CheckboxGroupParent(
                groupId,
                AllValues,
                DefaultValue,
                () => CurrentValue,
                SetValueInternal);
        }

        return new CheckboxGroupContext(
            Value: CurrentValue,
            DefaultValue: DefaultValue,
            AllValues: AllValues,
            Disabled: ResolvedDisabled,
            Parent: parent,
            Validation: FieldContext?.Validation,
            SetValueFunc: SetValueInternal,
            RegisterControlAction: RegisterControl);
    }

    private void RegisterControl(CheckboxRoot checkbox)
    {
        if (controlRef is null && !checkbox.Parent)
        {
            controlRef = checkbox;
            FieldContext?.RegisterFocusHandlerFunc(async () =>
            {
                if (controlRef?.Element is not null)
                {
                    await controlRef.Element.Value.FocusAsync();
                }
            });
        }
    }

    private async Task SetValueInternal(string[] newValue)
    {
        var eventArgs = new CheckboxGroupValueChangeEventArgs(newValue);

        if (OnValueChange.HasDelegate)
        {
            await OnValueChange.InvokeAsync(eventArgs);

            if (eventArgs.IsCanceled)
            {
                return;
            }
        }

        if (!IsControlled)
        {
            internalValue = newValue;
        }

        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(newValue);
        }

        context = CreateContext();
        await HandleValueChangedAsync();
        StateHasChanged();
    }

    private async Task HandleValueChangedAsync()
    {
        var currentValue = CurrentValue ?? [];

        FormContext?.ClearErrors(ResolvedName);

        var initialValue = FieldContext?.ValidityData.InitialValue;
        var isDirty = initialValue is string[] initial ? !ArraysEqual(currentValue, initial) : currentValue.Length > 0;

        FieldContext?.SetDirty(isDirty);
        FieldContext?.SetFilled(currentValue.Length > 0);

        if (FieldContext?.ShouldValidateOnChangeFunc() == true)
        {
            await FieldContext.Validation.CommitAsync(currentValue);
        }
        else
        {
            await (FieldContext?.Validation.CommitAsync(currentValue, revalidateOnly: true) ?? Task.CompletedTask);
        }
    }

    private static bool ArraysEqual(string[]? a, string[]? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        if (a.Length != b.Length)
        {
            return false;
        }

        var sortedA = a.OrderBy(x => x).ToArray();
        var sortedB = b.OrderBy(x => x).ToArray();

        return sortedA.SequenceEqual(sortedB);
    }
}
