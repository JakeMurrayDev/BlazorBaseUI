using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using BlazorBaseUI.Collapsible;
using BlazorBaseUI.Utilities.CompositeList;

namespace BlazorBaseUI.Accordion;

public sealed class AccordionItem<TValue> : ComponentBase, IReferencableComponent, IDisposable where TValue : notnull
{
    private const string DefaultTag = "div";

    private int index = -1;
    private string panelId = string.Empty;
    private TValue resolvedValue = default!;
    private bool isComponentRenderAs;
    private AccordionItemContext<TValue>? itemContext;
    private CollapsibleRootContext? collapsibleContext;
    private AccordionItemState<TValue> state = null!;
    private TransitionStatus transitionStatus = TransitionStatus.Undefined;

    private bool ResolvedDisabled => Disabled || (RootContext?.Disabled ?? false);

    private bool IsOpen => RootContext?.IsValueOpen(resolvedValue) ?? false;

    private AccordionRootContext<TValue>? TypedRootContext => RootContext as AccordionRootContext<TValue>;

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
                currentOpen,
                transitionStatus);

            itemContext = new AccordionItemContext<TValue>(
                TypedRootContext,
                resolvedValue,
                index,
                ResolvedDisabled,
                HandleTrigger,
                SetPanelId);

            collapsibleContext = new CollapsibleRootContext(
                Open: currentOpen,
                Disabled: ResolvedDisabled,
                TransitionStatus: transitionStatus,
                PanelId: panelId,
                HandleTrigger: HandleTrigger,
                HandleBeforeMatch: HandleBeforeMatch,
                SetPanelId: SetPanelId,
                SetTransitionStatus: SetTransitionStatus);
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && ListContext is not null && Element.HasValue)
        {
            index = ListContext.Register(Element.Value);
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
                SetPanelId);
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
                TransitionStatus: transitionStatus,
                PanelId: panelId,
                HandleTrigger: HandleTrigger,
                HandleBeforeMatch: HandleBeforeMatch,
                SetPanelId: SetPanelId,
                SetTransitionStatus: SetTransitionStatus);
        }
        else if (collapsibleContext.Open != currentOpen || collapsibleContext.Disabled != currentDisabled || collapsibleContext.TransitionStatus != transitionStatus)
        {
            collapsibleContext = collapsibleContext with { Open = currentOpen, Disabled = currentDisabled, TransitionStatus = transitionStatus };
        }

        if (state is null)
        {
            state = new AccordionItemState<TValue>(
                TypedRootContext.Value,
                currentDisabled,
                TypedRootContext.Orientation,
                index,
                currentOpen,
                transitionStatus);
        }
        else if (state.Open != currentOpen || state.Disabled != currentDisabled || state.Index != index || state.Orientation != TypedRootContext.Orientation || state.TransitionStatus != transitionStatus)
        {
            state = state with
            {
                Value = TypedRootContext.Value,
                Open = currentOpen,
                Disabled = currentDisabled,
                Index = index,
                Orientation = TypedRootContext.Orientation,
                TransitionStatus = transitionStatus
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
                    collapsibleBuilder.OpenRegion(0);
                    collapsibleBuilder.OpenComponent(0, RenderAs!);
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

                    if (state.TransitionStatus == TransitionStatus.Starting)
                    {
                        collapsibleBuilder.AddAttribute(7, "data-starting-style", string.Empty);
                    }
                    if (state.TransitionStatus == TransitionStatus.Ending)
                    {
                        collapsibleBuilder.AddAttribute(8, "data-ending-style", string.Empty);
                    }

                    if (!string.IsNullOrEmpty(resolvedClass))
                    {
                        collapsibleBuilder.AddAttribute(9, "class", resolvedClass);
                    }
                    if (!string.IsNullOrEmpty(resolvedStyle))
                    {
                        collapsibleBuilder.AddAttribute(10, "style", resolvedStyle);
                    }

                    collapsibleBuilder.AddAttribute(11, "ChildContent", ChildContent);
                    collapsibleBuilder.AddComponentReferenceCapture(12, component => { Element = ((IReferencableComponent)component).Element; });
                    collapsibleBuilder.CloseComponent();
                    collapsibleBuilder.CloseRegion();
                }
                else
                {
                    collapsibleBuilder.OpenRegion(1);
                    collapsibleBuilder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
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

                    if (state.TransitionStatus == TransitionStatus.Starting)
                    {
                        collapsibleBuilder.AddAttribute(7, "data-starting-style", string.Empty);
                    }
                    if (state.TransitionStatus == TransitionStatus.Ending)
                    {
                        collapsibleBuilder.AddAttribute(8, "data-ending-style", string.Empty);
                    }

                    if (!string.IsNullOrEmpty(resolvedClass))
                    {
                        collapsibleBuilder.AddAttribute(9, "class", resolvedClass);
                    }
                    if (!string.IsNullOrEmpty(resolvedStyle))
                    {
                        collapsibleBuilder.AddAttribute(10, "style", resolvedStyle);
                    }

                    collapsibleBuilder.AddElementReferenceCapture(11, e => Element = e);
                    collapsibleBuilder.AddContent(12, ChildContent);
                    collapsibleBuilder.CloseElement();
                    collapsibleBuilder.CloseRegion();
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

    private void SetTransitionStatus(TransitionStatus status)
    {
        if (transitionStatus != status)
        {
            transitionStatus = status;
            if (collapsibleContext is not null)
            {
                collapsibleContext = collapsibleContext with { TransitionStatus = status };
            }
            StateHasChanged();
        }
    }

    private void HandleTrigger()
    {
        if (ResolvedDisabled)
        {
            return;
        }

        var nextOpen = !IsOpen;
        var args = new CollapsibleOpenChangeEventArgs(nextOpen, CollapsibleOpenChangeReason.TriggerPress);

        _ = OnOpenChange.InvokeAsync(args);

        if (args.Canceled)
        {
            return;
        }

        RootContext?.HandleValueChange(resolvedValue, nextOpen);
    }

    private void HandleBeforeMatch()
    {
        // beforematch event should only open, not toggle
        if (IsOpen || ResolvedDisabled)
        {
            return;
        }

        var args = new CollapsibleOpenChangeEventArgs(true, CollapsibleOpenChangeReason.None);

        _ = OnOpenChange.InvokeAsync(args);

        if (args.Canceled)
        {
            return;
        }

        RootContext?.HandleValueChange(resolvedValue, true);
    }

    public void Dispose()
    {
        if (index >= 0)
        {
            ListContext?.Unregister(index);
        }
    }
}
