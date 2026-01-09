using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Utilities.CompositeList;
using BlazorBaseUI.DirectionProvider;

namespace BlazorBaseUI.Accordion;

public sealed class AccordionRoot<TValue> : ComponentBase where TValue : notnull
{
    private const string DefaultTag = "div";

    private TValue[] currentValue = [];
    private bool isComponentRenderAs;
    private AccordionRootContext<TValue> context = null!;
    private AccordionRootState<TValue> state = null!;

    [CascadingParameter]
    private DirectionProviderContext? DirectionContext { get; set; }

    [Parameter]
    public TValue[]? Value { get; set; }

    [Parameter]
    public TValue[] DefaultValue { get; set; } = [];

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool Multiple { get; set; }

    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Vertical;

    [Parameter]
    public bool LoopFocus { get; set; } = true;

    [Parameter]
    public bool HiddenUntilFound { get; set; }

    [Parameter]
    public bool KeepMounted { get; set; }

    [Parameter]
    public EventCallback<TValue[]> ValueChanged { get; set; }

    [Parameter]
    public EventCallback<AccordionValueChangeEventArgs<TValue>> OnValueChange { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<AccordionRootState<TValue>, string>? ClassValue { get; set; }

    [Parameter]
    public Func<AccordionRootState<TValue>, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool IsControlled => Value is not null;

    private TValue[] CurrentValue => IsControlled ? Value! : currentValue;

    protected override void OnInitialized()
    {
        currentValue = DefaultValue;
        var direction = DirectionContext?.Direction ?? Direction.Ltr;

        context = new AccordionRootContext<TValue>(
            Value: CurrentValue,
            Disabled: Disabled,
            Orientation: Orientation,
            Direction: direction,
            LoopFocus: LoopFocus,
            HiddenUntilFound: HiddenUntilFound,
            KeepMounted: KeepMounted,
            OnValueChange: HandleValueChange);

        state = new AccordionRootState<TValue>(CurrentValue, Disabled, Orientation);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var direction = DirectionContext?.Direction ?? Direction.Ltr;

        if (!ReferenceEquals(context.Value, CurrentValue) ||
            context.Disabled != Disabled ||
            context.Orientation != Orientation ||
            context.Direction != direction ||
            context.LoopFocus != LoopFocus ||
            context.HiddenUntilFound != HiddenUntilFound ||
            context.KeepMounted != KeepMounted)
        {
            context = context with
            {
                Value = CurrentValue,
                Disabled = Disabled,
                Orientation = Orientation,
                Direction = direction,
                LoopFocus = LoopFocus,
                HiddenUntilFound = HiddenUntilFound,
                KeepMounted = KeepMounted
            };
        }

        if (!ReferenceEquals(state.Value, CurrentValue) || state.Disabled != Disabled || state.Orientation != Orientation)
        {
            state = state with { Value = CurrentValue, Disabled = Disabled, Orientation = Orientation };
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var direction = DirectionContext?.Direction ?? Direction.Ltr;

        builder.OpenComponent<CascadingValue<IAccordionRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(innerBuilder =>
        {
            innerBuilder.OpenComponent<CompositeListProvider>(0);
            innerBuilder.AddComponentParameter(1, "ChildContent", (RenderFragment)(listBuilder =>
            {
                if (isComponentRenderAs)
                {
                    listBuilder.OpenComponent(0, RenderAs!);
                }
                else
                {
                    listBuilder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
                }

                listBuilder.AddMultipleAttributes(1, AdditionalAttributes);

                listBuilder.AddAttribute(2, "dir", direction.ToAttributeString());
                listBuilder.AddAttribute(3, "role", "region");
                listBuilder.AddAttribute(4, "data-orientation", state.Orientation.ToDataAttributeString());

                if (state.Disabled)
                {
                    listBuilder.AddAttribute(5, "data-disabled", string.Empty);
                }

                if (!string.IsNullOrEmpty(resolvedClass))
                {
                    listBuilder.AddAttribute(6, "class", resolvedClass);
                }
                if (!string.IsNullOrEmpty(resolvedStyle))
                {
                    listBuilder.AddAttribute(7, "style", resolvedStyle);
                }

                if (isComponentRenderAs)
                {
                    listBuilder.AddAttribute(8, "ChildContent", ChildContent);
                    listBuilder.AddComponentReferenceCapture(9, component => { Element = ((IReferencableComponent)component).Element; });
                    listBuilder.CloseComponent();
                }
                else
                {
                    listBuilder.AddElementReferenceCapture(10, e => Element = e);
                    listBuilder.AddContent(11, ChildContent);
                    listBuilder.CloseElement();
                }
            }));
            innerBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private void HandleValueChange(TValue itemValue, bool nextOpen)
    {
        TValue[] nextValue;

        if (!Multiple)
        {
            nextValue = CurrentValue.Length > 0 && EqualityComparer<TValue>.Default.Equals(CurrentValue[0], itemValue)
                ? []
                : [itemValue];
        }
        else if (nextOpen)
        {
            nextValue = [.. CurrentValue, itemValue];
        }
        else
        {
            nextValue = [.. CurrentValue.Where(v => !EqualityComparer<TValue>.Default.Equals(v, itemValue))];
        }

        var args = new AccordionValueChangeEventArgs<TValue>(nextValue);
        _ = OnValueChange.InvokeAsync(args);

        if (args.Canceled)
        {
            return;
        }

        if (!IsControlled)
        {
            currentValue = nextValue;
        }

        _ = ValueChanged.InvokeAsync(nextValue);

        var direction = DirectionContext?.Direction ?? Direction.Ltr;
        context = context with { Value = nextValue };
        state = state with { Value = nextValue };

        StateHasChanged();
    }
}
