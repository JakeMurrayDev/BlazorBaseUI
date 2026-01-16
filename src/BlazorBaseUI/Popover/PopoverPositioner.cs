using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Popover;

public sealed class PopoverPositioner : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "div";

    private Lazy<Task<IJSObjectReference>>? moduleTask;
    private bool hasRendered;
    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;
    private ElementReference? arrowElement;
    private string? positionerId;
    private PopoverPositionerState state;
    private PopoverPositionerContext positionerContext = null!;

    private Lazy<Task<IJSObjectReference>> ModuleTask => moduleTask ??= new Lazy<Task<IJSObjectReference>>(() =>
        JSRuntime!.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorBaseUI/blazor-baseui-popover.js").AsTask());

    [Inject]
    private IJSRuntime? JSRuntime { get; set; }

    [CascadingParameter]
    private PopoverRootContext? RootContext { get; set; }

    [CascadingParameter]
    private PopoverPortalContext? PortalContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Side Side { get; set; } = Side.Bottom;

    [Parameter]
    public Align Align { get; set; } = Align.Center;

    [Parameter]
    public int SideOffset { get; set; }

    [Parameter]
    public int AlignOffset { get; set; }

    [Parameter]
    public int CollisionPadding { get; set; } = 5;

    [Parameter]
    public int ArrowPadding { get; set; } = 5;

    [Parameter]
    public bool Sticky { get; set; }

    [Parameter]
    public bool DisableAnchorTracking { get; set; }

    [Parameter]
    public PositionMethod PositionMethod { get; set; } = PositionMethod.Fixed;

    [Parameter]
    public ElementReference? Anchor { get; set; }

    [Parameter]
    public Func<PopoverPositionerState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<PopoverPositionerState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        positionerContext = CreatePositionerContext();
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var open = RootContext?.GetOpen() ?? false;
        state = new PopoverPositionerState(open, Side, Align, false);

        positionerContext.Side = Side;
        positionerContext.Align = Align;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            RootContext?.SetPositionerElement(Element);
        }

        if (hasRendered && Element.HasValue && RootContext?.GetMounted() == true)
        {
            if (string.IsNullOrEmpty(positionerId))
            {
                await InitializePositionerAsync();
            }
            else if (!DisableAnchorTracking)
            {
                await UpdatePositionAsync();
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null)
        {
            return;
        }

        var open = RootContext.GetOpen();
        var mounted = RootContext.GetMounted();
        var transitionStatus = RootContext.TransitionStatus;
        var instantType = RootContext.InstantType;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        builder.OpenComponent<CascadingValue<PopoverPositionerContext>>(0);
        builder.AddComponentParameter(1, "Value", positionerContext);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(innerBuilder =>
        {
            if (isComponentRenderAs)
            {
                innerBuilder.OpenComponent(0, RenderAs!);
            }
            else
            {
                innerBuilder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            }

            innerBuilder.AddMultipleAttributes(1, AdditionalAttributes);
            innerBuilder.AddAttribute(2, "role", "presentation");

            if (!mounted)
            {
                innerBuilder.AddAttribute(3, "hidden", true);
            }

            innerBuilder.AddAttribute(4, "data-side", Side.ToDataAttributeString());
            innerBuilder.AddAttribute(5, "data-align", Align.ToDataAttributeString());

            if (open)
            {
                innerBuilder.AddAttribute(6, "data-open", string.Empty);
            }
            else
            {
                innerBuilder.AddAttribute(7, "data-closed", string.Empty);
            }

            if (state.AnchorHidden)
            {
                innerBuilder.AddAttribute(8, "data-anchor-hidden", string.Empty);
            }

            var instantAttr = instantType.ToDataAttributeString();
            if (!string.IsNullOrEmpty(instantAttr))
            {
                innerBuilder.AddAttribute(9, "data-instant", instantAttr);
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                innerBuilder.AddAttribute(10, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                innerBuilder.AddAttribute(11, "style", resolvedStyle);
            }

            if (isComponentRenderAs)
            {
                innerBuilder.AddAttribute(12, "ChildContent", ChildContent);
                innerBuilder.AddComponentReferenceCapture(13, component =>
                {
                    componentReference = (IReferencableComponent)component;
                    var newElement = componentReference.Element;
                    if (!Nullable.Equals(Element, newElement))
                    {
                        Element = newElement;
                        RootContext?.SetPositionerElement(Element);
                    }
                });
                innerBuilder.CloseComponent();
            }
            else
            {
                innerBuilder.AddContent(14, ChildContent);
                innerBuilder.AddElementReferenceCapture(15, elementReference =>
                {
                    if (!Nullable.Equals(Element, elementReference))
                    {
                        Element = elementReference;
                        RootContext?.SetPositionerElement(Element);
                    }
                });
                innerBuilder.CloseElement();
            }
        }));
        builder.CloseComponent();
    }

    private PopoverPositionerContext CreatePositionerContext() => new(
        side: Side,
        align: Align,
        anchorHidden: false,
        arrowUncentered: false,
        getArrowElement: () => arrowElement,
        setArrowElement: SetArrowElement);

    private void SetArrowElement(ElementReference? element)
    {
        if (Nullable.Equals(arrowElement, element))
        {
            return;
        }

        arrowElement = element;

        if (hasRendered && !string.IsNullOrEmpty(positionerId))
        {
            _ = UpdatePositionAsync();
        }
    }

    private async Task InitializePositionerAsync()
    {
        if (!Element.HasValue || RootContext is null)
        {
            return;
        }

        var anchorElement = Anchor ?? RootContext.GetTriggerElement();
        if (!anchorElement.HasValue)
        {
            return;
        }

        try
        {
            var module = await ModuleTask.Value;
            positionerId = await module.InvokeAsync<string>(
                "initializePositioner",
                Element.Value,
                anchorElement.Value,
                Side.ToDataAttributeString(),
                Align.ToDataAttributeString(),
                SideOffset,
                AlignOffset,
                CollisionPadding,
                ArrowPadding,
                arrowElement,
                Sticky,
                PositionMethod == PositionMethod.Fixed ? "fixed" : "absolute",
                DisableAnchorTracking);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UpdatePositionAsync()
    {
        if (string.IsNullOrEmpty(positionerId) || RootContext is null)
        {
            return;
        }

        var anchorElement = Anchor ?? RootContext.GetTriggerElement();
        if (!anchorElement.HasValue)
        {
            return;
        }

        try
        {
            var module = await ModuleTask.Value;
            await module.InvokeVoidAsync(
                "updatePosition",
                positionerId,
                anchorElement.Value,
                Side.ToDataAttributeString(),
                Align.ToDataAttributeString(),
                SideOffset,
                AlignOffset,
                CollisionPadding,
                ArrowPadding,
                arrowElement,
                Sticky,
                PositionMethod == PositionMethod.Fixed ? "fixed" : "absolute");
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask?.IsValueCreated == true && hasRendered && !string.IsNullOrEmpty(positionerId))
        {
            try
            {
                var module = await ModuleTask.Value;
                await module.InvokeVoidAsync("disposePositioner", positionerId);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }
}
