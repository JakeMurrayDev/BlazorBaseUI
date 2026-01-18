using BlazorBaseUI.Popover;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Tooltip;

public sealed class TooltipPopup : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "div";

    private Lazy<Task<IJSObjectReference>>? moduleTask;
    private bool hasRendered;
    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;
    private TooltipPopupState state;
    private DotNetObjectReference<TooltipPopup>? dotNetRef;
    private CancellationTokenSource? hoverCts;

    private Lazy<Task<IJSObjectReference>> ModuleTask => moduleTask ??= new Lazy<Task<IJSObjectReference>>(() =>
        JSRuntime!.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorBaseUI/blazor-baseui-tooltip.js").AsTask());

    [Inject]
    private IJSRuntime? JSRuntime { get; set; }

    [CascadingParameter]
    private TooltipRootContext? RootContext { get; set; }

    [CascadingParameter]
    private TooltipPositionerContext? PositionerContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<TooltipPopupState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<TooltipPopupState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var open = RootContext?.GetOpen() ?? false;
        var side = PositionerContext?.Side ?? Side.Top;
        var align = PositionerContext?.Align ?? Align.Center;
        var instant = RootContext?.InstantType ?? TooltipInstantType.None;
        var transitionStatus = RootContext?.TransitionStatus ?? Popover.TransitionStatus.None;
        state = new TooltipPopupState(open, side, align, instant, transitionStatus);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            dotNetRef = DotNetObjectReference.Create(this);
            RootContext?.SetPopupElement(Element);
            await InitializePopupAsync();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null)
        {
            return;
        }

        var open = RootContext.GetOpen();
        var transitionStatus = RootContext.TransitionStatus;
        var instantType = RootContext.InstantType;
        var disableHoverablePopup = RootContext.DisableHoverablePopup;
        var popupId = RootContext.PopupId;
        var side = PositionerContext?.Side ?? Side.Top;
        var align = PositionerContext?.Align ?? Align.Center;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "role", "tooltip");
        builder.AddAttribute(3, "id", popupId);

        builder.AddAttribute(4, "data-side", side.ToDataAttributeString());
        builder.AddAttribute(5, "data-align", align.ToDataAttributeString());

        if (open)
        {
            builder.AddAttribute(6, "data-open", string.Empty);
        }
        else
        {
            builder.AddAttribute(7, "data-closed", string.Empty);
        }

        var instantAttr = instantType.ToDataAttributeString();
        if (!string.IsNullOrEmpty(instantAttr))
        {
            builder.AddAttribute(8, "data-instant", instantAttr);
        }

        if (transitionStatus == Popover.TransitionStatus.Starting)
        {
            builder.AddAttribute(9, "data-starting-style", string.Empty);
        }
        else if (transitionStatus == Popover.TransitionStatus.Ending)
        {
            builder.AddAttribute(10, "data-ending-style", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(11, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(12, "style", resolvedStyle);
        }

        builder.AddAttribute(13, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));

        if (!disableHoverablePopup)
        {
            builder.AddAttribute(14, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnterAsync));
            builder.AddAttribute(15, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseLeaveAsync));
        }

        if (isComponentRenderAs)
        {
            builder.AddAttribute(16, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(17, component =>
            {
                componentReference = (IReferencableComponent)component;
                var newElement = componentReference.Element;
                if (!Nullable.Equals(Element, newElement))
                {
                    Element = newElement;
                    RootContext?.SetPopupElement(Element);
                }
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddContent(18, ChildContent);
            builder.AddElementReferenceCapture(19, elementReference =>
            {
                if (!Nullable.Equals(Element, elementReference))
                {
                    Element = elementReference;
                    RootContext?.SetPopupElement(Element);
                }
            });
            builder.CloseElement();
        }
    }

    [JSInvokable]
    public void OnTransitionEnd()
    {
    }

    public async ValueTask DisposeAsync()
    {
        CancelHoverDelay();

        if (moduleTask?.IsValueCreated == true && hasRendered && Element.HasValue)
        {
            try
            {
                var module = await ModuleTask.Value;
                await module.InvokeVoidAsync("disposePopup", Element.Value);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
                // Circuit-safe: intentionally empty to prevent crashes during Hot Reload or disconnection
            }
        }

        dotNetRef?.Dispose();
    }

    private async Task InitializePopupAsync()
    {
        if (!Element.HasValue || RootContext is null)
        {
            return;
        }

        try
        {
            var module = await ModuleTask.Value;
            await module.InvokeVoidAsync("initializePopup", Element.Value, dotNetRef);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
            // Circuit-safe: intentionally empty to prevent crashes during Hot Reload or disconnection
        }
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (RootContext is null)
        {
            return;
        }

        if (e.Key == "Escape")
        {
            await RootContext.SetOpenAsync(false, TooltipOpenChangeReason.EscapeKey, null);
        }

        await EventUtilities.InvokeOnKeyDownAsync(AdditionalAttributes, e);
    }

    private Task HandleMouseEnterAsync(MouseEventArgs e)
    {
        CancelHoverDelay();
        return Task.CompletedTask;
    }

    private async Task HandleMouseLeaveAsync(MouseEventArgs e)
    {
        if (RootContext is null)
        {
            return;
        }

        CancelHoverDelay();

        if (!RootContext.GetOpen())
        {
            return;
        }

        hoverCts = new CancellationTokenSource();
        var token = hoverCts.Token;

        try
        {
            await Task.Delay(100, token);
            if (!token.IsCancellationRequested)
            {
                await RootContext.SetOpenAsync(false, TooltipOpenChangeReason.TriggerHover, null);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private void CancelHoverDelay()
    {
        if (hoverCts is not null)
        {
            hoverCts.Cancel();
            hoverCts.Dispose();
            hoverCts = null;
        }
    }
}
