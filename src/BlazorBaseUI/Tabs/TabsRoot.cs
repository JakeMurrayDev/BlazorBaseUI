using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tabs;

public sealed class TabsRoot<TValue> : ComponentBase
{
    private const string DefaultTag = "div";

    private TValue? internalValue;
    private TValue? previousValue;
    private bool hasExplicitDefaultValue;
    private ActivationDirection activationDirection = ActivationDirection.None;
    private TabsRootContext<TValue>? rootContext;
    private TabsRootState? cachedState;
    private bool stateDirty = true;

    [Parameter]
    public TValue? Value { get; set; }

    [Parameter]
    public TValue? DefaultValue { get; set; }

    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    [Parameter]
    public EventCallback<TValue?> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<TabsValueChangeEventArgs<TValue>> OnValueChange { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<TabsRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<TabsRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private bool IsControlled => ValueChanged.HasDelegate;

    private TValue? CurrentValue => IsControlled ? Value : internalValue;

    private TabsRootState State
    {
        get
        {
            if (stateDirty || cachedState is null)
            {
                cachedState = new TabsRootState(Orientation, activationDirection);
                stateDirty = false;
            }
            return cachedState;
        }
    }

    protected override void OnInitialized()
    {
        hasExplicitDefaultValue = DefaultValue is not null;

        if (!IsControlled)
        {
            internalValue = hasExplicitDefaultValue ? DefaultValue : default;
        }

        previousValue = CurrentValue;
        rootContext = CreateContext();
    }

    protected override void OnParametersSet()
    {
        if (!EqualityComparer<TValue>.Default.Equals(CurrentValue, previousValue))
        {
            previousValue = CurrentValue;
            stateDirty = true;
        }

        rootContext?.UpdateProperties(Orientation, activationDirection);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<ITabsRootContext<TValue>>>(0);
        builder.AddComponentParameter(1, "Value", rootContext);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)RenderRoot);
        builder.CloseComponent();
    }

    private void RenderRoot(RenderTreeBuilder builder)
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

    private Dictionary<string, object> BuildAttributes(TabsRootState state, string? resolvedClass, string? resolvedStyle)
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

        state.WriteDataAttributes(attributes);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        return attributes;
    }

    private TabsRootContext<TValue> CreateContext() => new(
        orientation: Orientation,
        activationDirection: activationDirection,
        getValue: () => CurrentValue,
        onValueChange: SetValueInternalAsync);

    private async Task SetValueInternalAsync(TValue? value, ActivationDirection direction)
    {
        var eventArgs = new TabsValueChangeEventArgs<TValue>(value, direction);

        if (OnValueChange.HasDelegate)
        {
            await OnValueChange.InvokeAsync(eventArgs);

            if (eventArgs.IsCanceled)
            {
                StateHasChanged();
                return;
            }
        }

        activationDirection = direction;
        stateDirty = true;

        if (!IsControlled)
        {
            internalValue = value;
        }

        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(value);
        }

        rootContext?.UpdateProperties(Orientation, activationDirection);
        StateHasChanged();
    }
}
