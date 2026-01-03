using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Utilities.CompositeList;
using BlazorBaseUI.DirectionProvider;

namespace BlazorBaseUI.Accordion;

public sealed class AccordionRoot<TValue> : ComponentBase where TValue : notnull
{
    private const string DefaultTag = "div";

    private TValue[] currentValue = [];
    private AccordionRootContext<TValue> context = null!;

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

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private bool IsControlled => Value is not null;

    private TValue[] CurrentValue => IsControlled ? Value! : currentValue;

    protected override void OnInitialized()
    {
        currentValue = DefaultValue;
        context = CreateContext();
    }

    protected override void OnParametersSet()
    {
        context = CreateContext();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = context.State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var attributes = BuildAttributes(state);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        builder.OpenComponent<CascadingValue<IAccordionRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(innerBuilder =>
        {
            innerBuilder.OpenComponent<CompositeListProvider>(4);
            innerBuilder.AddComponentParameter(5, "ChildContent", (RenderFragment)(listBuilder =>
            {
                if (RenderAs is not null)
                {
                    listBuilder.OpenComponent(6, RenderAs);
                    listBuilder.AddMultipleAttributes(7, attributes);
                    listBuilder.AddComponentParameter(8, "ChildContent", ChildContent);
                    listBuilder.CloseComponent();
                }
                else
                {
                    var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
                    listBuilder.OpenElement(9, tag);
                    listBuilder.AddMultipleAttributes(10, attributes);
                    listBuilder.AddElementReferenceCapture(11, e => Element = e);
                    listBuilder.AddContent(12, ChildContent);
                    listBuilder.CloseElement();
                }
            }));
            innerBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private Dictionary<string, object> BuildAttributes(AccordionRootState<TValue> state)
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

        attributes["dir"] = DirectionContext?.Direction.ToAttributeString()!;
        attributes["role"] = "region";

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }

    private AccordionRootContext<TValue> CreateContext() => new(
        Value: CurrentValue,
        Disabled: Disabled,
        Orientation: Orientation,
        Direction: DirectionContext?.Direction ?? Direction.Ltr,
        LoopFocus: LoopFocus,
        HiddenUntilFound: HiddenUntilFound,
        KeepMounted: KeepMounted,
        OnValueChange: HandleValueChange);

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
        OnValueChange.InvokeAsync(args);

        if (args.Canceled)
            return;

        if (!IsControlled)
        {
            currentValue = nextValue;
        }

        ValueChanged.InvokeAsync(nextValue);
        context = CreateContext();
        StateHasChanged();
    }
}