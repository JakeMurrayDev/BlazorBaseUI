using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Tabs;

public sealed class TabsRoot<TValue> : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private TValue? internalValue;
    private TValue? previousValue;
    private bool hasExplicitDefaultValue;
    private ActivationDirection activationDirection = ActivationDirection.None;
    private TabsRootContext<TValue>? rootContext;
    private TabsRootState state = TabsRootState.Default;
    private bool isComponentRenderAs;

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

    public ElementReference? Element { get; private set; }

    private bool IsControlled => ValueChanged.HasDelegate;

    private TValue? CurrentValue => IsControlled ? Value : internalValue;

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
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var currentValue = CurrentValue;
        if (!EqualityComparer<TValue>.Default.Equals(currentValue, previousValue))
        {
            previousValue = currentValue;
        }

        if (state.Orientation != Orientation || state.ActivationDirection != activationDirection)
        {
            state = new TabsRootState(Orientation, activationDirection);
        }

        if (rootContext is not null)
        {
            rootContext.Orientation = Orientation;
            rootContext.ActivationDirection = activationDirection;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<ITabsRootContext<TValue>>>(0);
        builder.AddComponentParameter(1, "Value", rootContext);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)BuildInnerContent);
        builder.CloseComponent();
    }

    private void BuildInnerContent(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientationValue = Orientation.ToDataAttributeString();

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);

        if (orientationValue is not null)
        {
            builder.AddAttribute(2, "data-orientation", orientationValue);
        }
        builder.AddAttribute(3, "data-activation-direction", activationDirection.ToDataAttributeString());

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(4, "class", resolvedClass);
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(5, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(6, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(7, component =>
            {
                Element = ((IReferencableComponent)component).Element;
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(6, elementReference => Element = elementReference);
            builder.AddContent(7, ChildContent);
            builder.CloseElement();
        }
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
        state = new TabsRootState(Orientation, activationDirection);

        if (!IsControlled)
        {
            internalValue = value;
        }

        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(value);
        }

        if (rootContext is not null)
        {
            rootContext.Orientation = Orientation;
            rootContext.ActivationDirection = activationDirection;
        }
        StateHasChanged();
    }
}
