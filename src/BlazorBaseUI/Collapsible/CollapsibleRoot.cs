using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorBaseUI.Collapsible;

public sealed class CollapsibleRoot : ComponentBase, IReferencableComponent
{
    private const string DefaultTag = "div";

    private bool isOpen;
    private string panelId = string.Empty;
    private bool isComponentRenderAs;
    private CollapsibleRootState state = new(false, false, TransitionStatus.Undefined);
    private CollapsibleRootContext context = null!;
    private TransitionStatus transitionStatus = TransitionStatus.Undefined;

    private bool IsControlled => Open.HasValue;

    private bool CurrentOpen => IsControlled ? Open!.Value : isOpen;

    [Parameter]
    public bool? Open { get; set; }

    [Parameter]
    public bool DefaultOpen { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public EventCallback<CollapsibleOpenChangeEventArgs> OnOpenChange { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<CollapsibleRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<CollapsibleRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        isOpen = DefaultOpen;
        context = new CollapsibleRootContext(
            CurrentOpen,
            Disabled,
            transitionStatus,
            panelId,
            HandleTrigger,
            HandleBeforeMatch,
            SetPanelId,
            SetTransitionStatus);
        state = new CollapsibleRootState(CurrentOpen, Disabled, transitionStatus);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (string.IsNullOrEmpty(panelId))
        {
            var id = AttributeUtilities.GetAttributeStringValue(AdditionalAttributes, "id");
            if (!string.IsNullOrEmpty(id))
            {
                SetPanelId(id);
            }
        }

        var currentOpen = CurrentOpen;
        if (state.Open != currentOpen || state.Disabled != Disabled || state.TransitionStatus != transitionStatus)
        {
            state = state with { Open = currentOpen, Disabled = Disabled, TransitionStatus = transitionStatus };
            context = context with { Open = currentOpen, Disabled = Disabled, TransitionStatus = transitionStatus };
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        builder.OpenComponent<CascadingValue<CollapsibleRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(innerBuilder =>
        {
            if (isComponentRenderAs)
            {
                innerBuilder.OpenRegion(0);
                innerBuilder.OpenComponent(0, RenderAs!);
                innerBuilder.AddMultipleAttributes(1, AdditionalAttributes);
                RenderDataAttributes(innerBuilder);
                RenderClassAndStyle(innerBuilder, resolvedClass, resolvedStyle);
                innerBuilder.AddAttribute(9, "ChildContent", ChildContent);
                innerBuilder.AddComponentReferenceCapture(10, component => { Element = ((IReferencableComponent)component).Element; });
                innerBuilder.CloseComponent();
                innerBuilder.CloseRegion();
            }
            else
            {
                innerBuilder.OpenRegion(1);
                innerBuilder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
                innerBuilder.AddMultipleAttributes(1, AdditionalAttributes);
                RenderDataAttributes(innerBuilder);
                RenderClassAndStyle(innerBuilder, resolvedClass, resolvedStyle);
                innerBuilder.AddElementReferenceCapture(9, elementReference => Element = elementReference);
                innerBuilder.AddContent(10, ChildContent);
                innerBuilder.CloseElement();
                innerBuilder.CloseRegion();
            }
        }));
        builder.CloseComponent();
    }

    internal void SetPanelId(string id)
    {
        if (panelId != id)
        {
            panelId = id;
            context = context with { PanelId = id };
            StateHasChanged();
        }
    }

    internal void SetTransitionStatus(TransitionStatus status)
    {
        if (transitionStatus != status)
        {
            transitionStatus = status;
            state = state with { TransitionStatus = status };
            context = context with { TransitionStatus = status };
            StateHasChanged();
        }
    }

    private void RenderDataAttributes(RenderTreeBuilder builder)
    {
        if (state.Open)
        {
            builder.AddAttribute(2, "data-open", string.Empty);
        }
        else
        {
            builder.AddAttribute(3, "data-closed", string.Empty);
        }

        if (state.TransitionStatus == TransitionStatus.Starting)
        {
            builder.AddAttribute(4, "data-starting-style", string.Empty);
        }

        if (state.TransitionStatus == TransitionStatus.Ending)
        {
            builder.AddAttribute(5, "data-ending-style", string.Empty);
        }
    }

    private void RenderClassAndStyle(RenderTreeBuilder builder, string? resolvedClass, string? resolvedStyle)
    {
        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(6, "class", resolvedClass);
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(7, "style", resolvedStyle);
        }
    }

    private void HandleTrigger()
    {
        if (Disabled)
        {
            return;
        }

        var nextOpen = !CurrentOpen;
        var args = new CollapsibleOpenChangeEventArgs(nextOpen, CollapsibleOpenChangeReason.TriggerPress);

        _ = OnOpenChange.InvokeAsync(args);

        if (args.Canceled)
        {
            return;
        }

        if (!IsControlled)
        {
            isOpen = nextOpen;
        }

        _ = OpenChanged.InvokeAsync(nextOpen);
        state = state with { Open = nextOpen };
        context = context with { Open = nextOpen };
        StateHasChanged();
    }

    private void HandleBeforeMatch()
    {
        // beforematch event should only open, not toggle
        if (CurrentOpen || Disabled)
        {
            return;
        }

        var args = new CollapsibleOpenChangeEventArgs(true, CollapsibleOpenChangeReason.None);

        _ = OnOpenChange.InvokeAsync(args);

        if (args.Canceled)
        {
            return;
        }

        if (!IsControlled)
        {
            isOpen = true;
        }

        _ = OpenChanged.InvokeAsync(true);
        state = state with { Open = true };
        context = context with { Open = true };
        StateHasChanged();
    }
}
