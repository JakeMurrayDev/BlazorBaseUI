using System.Diagnostics.CodeAnalysis;
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

    [CascadingParameter] private FieldRootContext? FieldContext { get; set; }

    [CascadingParameter] private FormContext? FormContext { get; set; }

    [CascadingParameter] private LabelableContext? LabelableContext { get; set; }

    [Parameter] public string[]? Value { get; set; }

    [Parameter] public string[]? DefaultValue { get; set; }

    [Parameter] public string[]? AllValues { get; set; }

    [Parameter] public bool Disabled { get; set; }

    [Parameter] public EventCallback<string[]> ValueChanged { get; set; }

    [Parameter] public EventCallback<CheckboxGroupValueChangeEventArgs> OnValueChange { get; set; }

    [Parameter] public string? As { get; set; }

    [Parameter] public Type? RenderAs { get; set; }

    [Parameter] public Func<CheckboxGroupState, string>? ClassValue { get; set; }

    [Parameter] public Func<CheckboxGroupState, string>? StyleValue { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull] public ElementReference? Element { get; private set; }

    private bool IsControlled => Value is not null;

    private string[]? CurrentValue => IsControlled ? Value : internalValue;

    private bool ResolvedDisabled => Disabled || (FieldContext?.Disabled ?? false);

    private string? ResolvedName => FieldContext?.Name;

    private FieldRootState FieldState => FieldContext?.State ?? FieldRootState.Default;

    private CheckboxGroupState State => CheckboxGroupState.FromFieldState(FieldState, ResolvedDisabled);

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
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!ArraysEqual(CurrentValue, previousValue))
        {
            previousValue = CurrentValue;
            await HandleValueChangedAsync();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        
        var context = CreateContext();

        builder.OpenComponent<CascadingValue<CheckboxGroupContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(RenderGroup));
        builder.CloseComponent();
    }

    public void NotifyStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        FieldContext?.UnsubscribeFunc(this);
    }

    private void RenderGroup(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(State));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(State));
        var attributes = BuildAttributes(State);

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

    private Dictionary<string, object> BuildAttributes(CheckboxGroupState state)
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

        attributes["id"] = groupId;
        attributes["role"] = "group";

        if (LabelableContext?.LabelId is not null)
            attributes["aria-labelledby"] = LabelableContext.LabelId;

        var describedBy = LabelableContext?.GetAriaDescribedBy();
        if (describedBy is not null)
            attributes["aria-describedby"] = describedBy;

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
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
                return;
        }

        if (!IsControlled)
        {
            internalValue = newValue;
        }

        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(newValue);
        }

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
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (a.Length != b.Length) return false;

        var sortedA = a.OrderBy(x => x).ToArray();
        var sortedB = b.OrderBy(x => x).ToArray();

        return sortedA.SequenceEqual(sortedB);
    }
}