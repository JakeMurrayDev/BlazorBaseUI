using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Collapsible;
using BlazorBaseUI.Utilities.CompositeList;

namespace BlazorBaseUI.Accordion;

public sealed class AccordionItem<TValue> : ComponentBase, IDisposable where TValue : notnull
{
    private const string DefaultTag = "div";

    private int index = -1;
    private string panelId = string.Empty;
    private TValue resolvedValue = default!;
    private bool isComponentRenderAs;
    private AccordionItemContext<TValue>? itemContext;
    private CollapsibleRootContext? collapsibleContext;
    private AccordionItemState<TValue> state = null!;
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

    public ElementReference? Element { get; private set; }

    private bool ResolvedDisabled => Disabled || (RootContext?.Disabled ?? false);

    private bool IsOpen => RootContext?.IsValueOpen(resolvedValue) ?? false;

    private AccordionRootContext<TValue>? TypedRootContext => RootContext as AccordionRootContext<TValue>;

    protected override void OnInitialized()
    {
        resolvedValue = ResolveValue();

        if (TypedRootContext is not null)
        {
            var currentOpen = IsOpen;
            state = new AccordionItemState<TValue>(
                TypedRootContext.Value,
                ResolvedDisabled,
                TypedRootContext.Orientation,
                index,
                currentOpen);

            itemContext = new AccordionItemContext<TValue>(
                TypedRootContext,
                resolvedValue,
                index,
                ResolvedDisabled,
                HandleTrigger,
                SetPanelId,
                SetTriggerId);

            collapsibleContext = new CollapsibleRootContext(
                Open: currentOpen,
                Disabled: ResolvedDisabled,
                PanelId: panelId,
                HandleTrigger: HandleTrigger,
                SetPanelId: SetPanelId);
        }
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
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (TypedRootContext is null)
        {
            return;
        }

        var currentOpen = IsOpen;
        var currentDisabled = ResolvedDisabled;

        if (itemContext is null)
        {
            itemContext = new AccordionItemContext<TValue>(
                TypedRootContext,
                resolvedValue,
                index,
                currentDisabled,
                HandleTrigger,
                SetPanelId,
                SetTriggerId);
        }
        else if (!ReferenceEquals(itemContext.RootContext, TypedRootContext) ||
                 itemContext.Index != index ||
                 itemContext.Disabled != currentDisabled)
        {
            itemContext = itemContext with
            {
                RootContext = TypedRootContext,
                Index = index,
                Disabled = currentDisabled
            };
        }

        if (collapsibleContext is null)
        {
            collapsibleContext = new CollapsibleRootContext(
                Open: currentOpen,
                Disabled: currentDisabled,
                PanelId: panelId,
                HandleTrigger: HandleTrigger,
                SetPanelId: SetPanelId);
        }
        else if (collapsibleContext.Open != currentOpen || collapsibleContext.Disabled != currentDisabled)
        {
            collapsibleContext = collapsibleContext with { Open = currentOpen, Disabled = currentDisabled };
        }

        if (state is null)
        {
            state = new AccordionItemState<TValue>(
                TypedRootContext.Value,
                currentDisabled,
                TypedRootContext.Orientation,
                index,
                currentOpen);
        }
        else if (state.Open != currentOpen || state.Disabled != currentDisabled || state.Index != index || state.Orientation != TypedRootContext.Orientation)
        {
            state = state with
            {
                Value = TypedRootContext.Value,
                Open = currentOpen,
                Disabled = currentDisabled,
                Index = index,
                Orientation = TypedRootContext.Orientation
            };
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null || itemContext is null || collapsibleContext is null)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        builder.OpenComponent<CascadingValue<IAccordionItemContext>>(0);
        builder.AddComponentParameter(1, "Value", itemContext);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(itemBuilder =>
        {
            itemBuilder.OpenComponent<CascadingValue<CollapsibleRootContext>>(0);
            itemBuilder.AddComponentParameter(1, "Value", collapsibleContext);
            itemBuilder.AddComponentParameter(2, "IsFixed", false);
            itemBuilder.AddComponentParameter(3, "ChildContent", (RenderFragment)(collapsibleBuilder =>
            {
                if (isComponentRenderAs)
                {
                    collapsibleBuilder.OpenComponent(0, RenderAs!);
                }
                else
                {
                    collapsibleBuilder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
                }

                collapsibleBuilder.AddMultipleAttributes(1, AdditionalAttributes);

                collapsibleBuilder.AddAttribute(2, "data-index", index.ToString());
                collapsibleBuilder.AddAttribute(3, "data-orientation", state.Orientation.ToDataAttributeString());

                if (state.Open)
                {
                    collapsibleBuilder.AddAttribute(4, "data-open", string.Empty);
                }
                else
                {
                    collapsibleBuilder.AddAttribute(5, "data-closed", string.Empty);
                }

                if (state.Disabled)
                {
                    collapsibleBuilder.AddAttribute(6, "data-disabled", string.Empty);
                }

                if (!string.IsNullOrEmpty(resolvedClass))
                {
                    collapsibleBuilder.AddAttribute(7, "class", resolvedClass);
                }
                if (!string.IsNullOrEmpty(resolvedStyle))
                {
                    collapsibleBuilder.AddAttribute(8, "style", resolvedStyle);
                }

                if (isComponentRenderAs)
                {
                    collapsibleBuilder.AddAttribute(9, "ChildContent", ChildContent);
                    collapsibleBuilder.AddComponentReferenceCapture(10, component => { Element = ((IReferencableComponent)component).Element; });
                    collapsibleBuilder.CloseComponent();
                }
                else
                {
                    collapsibleBuilder.AddElementReferenceCapture(11, e => element = e);
                    collapsibleBuilder.AddContent(12, ChildContent);
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
        {
            return Value;
        }

        if (typeof(TValue) == typeof(string))
        {
            return (TValue)(object)Guid.NewGuid().ToIdString();
        }

        throw new InvalidOperationException(
            $"AccordionItem requires a Value when TValue is '{typeof(TValue).Name}'. " +
            "Auto-generation is only supported for string type.");
    }

    private void SetPanelId(string id)
    {
        panelId = id;
        if (collapsibleContext is not null)
        {
            collapsibleContext = collapsibleContext with { PanelId = id };
        }
    }

    private void SetTriggerId(string id)
    {
    }

    private void HandleTrigger()
    {
        if (ResolvedDisabled)
        {
            return;
        }

        var nextOpen = !IsOpen;
        var args = new CollapsibleOpenChangeEventArgs(nextOpen);

        _ = OnOpenChange.InvokeAsync(args);

        if (args.Canceled)
        {
            return;
        }

        RootContext?.HandleValueChange(resolvedValue, nextOpen);
    }

    public void Dispose()
    {
        if (index >= 0)
        {
            ListContext?.Unregister(index);
        }
    }
}
