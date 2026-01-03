using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Collapsible;
using BlazorBaseUI.Utilities.CompositeList;

namespace BlazorBaseUI.Accordion;

public sealed class AccordionItem<TValue> : ComponentBase, IDisposable where TValue : notnull
{
    private const string DefaultTag = "div";

    private int index = -1;
    private string panelId = null!;
    private TValue resolvedValue = default!;
    private AccordionItemContext<TValue>? itemContext;
    private CollapsibleRootContext? collapsibleContext;
    private ElementReference? element;

    [CascadingParameter]
    private IAccordionRootContext? RootContext { get; set; }

    [CascadingParameter]
    private ICompositeListContext? ListContext { get; set; }

    [Parameter]
    public TValue? Value { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback<CollapsibleOpenChangeEventArgs> OnOpenChange { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<AccordionItemState<TValue>, string>? ClassValue { get; set; }

    [Parameter]
    public Func<AccordionItemState<TValue>, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private bool ResolvedDisabled => Disabled || (RootContext?.Disabled ?? false);

    private bool IsOpen => RootContext?.IsValueOpen(resolvedValue) ?? false;

    private AccordionRootContext<TValue>? TypedRootContext => RootContext as AccordionRootContext<TValue>;

    protected override void OnInitialized()
    {
        panelId = Guid.NewGuid().ToIdString();
        resolvedValue = ResolveValue();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && ListContext is not null && element.HasValue)
        {
            index = ListContext.Register(element.Value);
        }
    }

    protected override void OnParametersSet()
    {
        if (TypedRootContext is null)
            return;

        itemContext = new AccordionItemContext<TValue>(
            TypedRootContext,
            resolvedValue,
            index,
            ResolvedDisabled,
            panelId,
            HandleTrigger);

        collapsibleContext = new CollapsibleRootContext(
            Open: IsOpen,
            Disabled: ResolvedDisabled,
            PanelId: panelId,
            HandleTrigger: HandleTrigger);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null || itemContext is null || collapsibleContext is null)
            return;

        var state = itemContext.GetState();
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var attributes = BuildAttributes(state);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        builder.OpenComponent<CascadingValue<IAccordionItemContext>>(0);
        builder.AddComponentParameter(1, "Value", itemContext);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(itemBuilder =>
        {
            itemBuilder.OpenComponent<CascadingValue<CollapsibleRootContext>>(4);
            itemBuilder.AddComponentParameter(5, "Value", collapsibleContext);
            itemBuilder.AddComponentParameter(6, "IsFixed", false);
            itemBuilder.AddComponentParameter(7, "ChildContent", (RenderFragment)(collapsibleBuilder =>
            {
                if (RenderAs is not null)
                {
                    collapsibleBuilder.OpenComponent(8, RenderAs);
                    collapsibleBuilder.AddMultipleAttributes(9, attributes);
                    collapsibleBuilder.AddComponentParameter(10, "ChildContent", ChildContent);
                    collapsibleBuilder.CloseComponent();
                }
                else
                {
                    var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
                    collapsibleBuilder.OpenElement(11, tag);
                    collapsibleBuilder.AddMultipleAttributes(12, attributes);
                    collapsibleBuilder.AddElementReferenceCapture(13, e => element = e);
                    collapsibleBuilder.AddContent(14, ChildContent);
                    collapsibleBuilder.CloseElement();
                }
            }));
            itemBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private TValue ResolveValue()
    {
        if (Value is not null)
            return Value;

        if (typeof(TValue) == typeof(string))
            return (TValue)(object)Guid.NewGuid().ToIdString();

        throw new InvalidOperationException(
            $"AccordionItem requires a Value when TValue is '{typeof(TValue).Name}'. " +
            "Auto-generation is only supported for string type. " +
            "Either provide a Value or use AccordionItem without a type parameter (defaults to string).");
    }

    private Dictionary<string, object> BuildAttributes(AccordionItemState<TValue> state)
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

    private void HandleTrigger()
    {
        if (ResolvedDisabled)
            return;

        var nextOpen = !IsOpen;
        var args = new CollapsibleOpenChangeEventArgs(nextOpen);

        OnOpenChange.InvokeAsync(args);

        if (args.Canceled)
            return;

        RootContext?.HandleValueChange(resolvedValue, nextOpen);
    }

    public void Dispose()
    {
        if (index >= 0)
            ListContext?.Unregister(index);
    }
}