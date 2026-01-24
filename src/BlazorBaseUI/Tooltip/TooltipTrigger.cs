using BlazorBaseUI.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Tooltip;

/// <summary>
/// A tooltip trigger component that can work either nested inside a TooltipRoot
/// or detached with a handle for typed payloads.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the tooltip. Use object for untyped payloads.</typeparam>
public class TooltipTypedTrigger<TPayload> : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "button";

    private Lazy<Task<IJSObjectReference>>? moduleTask;
    private bool isComponentRenderAs;
    private bool hoverInitialized;
    private IReferencableComponent? componentReference;
    private string triggerId = null!;
    private TooltipTriggerState state;
    private CancellationTokenSource? hoverCts;
    private TooltipHandle<TPayload>? registeredHandle;

    private Lazy<Task<IJSObjectReference>> ModuleTask => moduleTask ??= new Lazy<Task<IJSObjectReference>>(() =>
        JSRuntime!.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorBaseUI/blazor-baseui-tooltip.js").AsTask());

    [Inject]
    private IJSRuntime? JSRuntime { get; set; }

    [CascadingParameter]
    private TooltipRootContext? RootContext { get; set; }

    [CascadingParameter]
    private TooltipProviderContext? ProviderContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public int Delay { get; set; } = 600;

    [Parameter]
    public int CloseDelay { get; set; }

    /// <summary>
    /// When true, uses JavaScript-based hover interaction with safePolygon support,
    /// allowing the user to move their cursor from the trigger to the tooltip popup
    /// without it closing. When false, uses C# Task.Delay for hover handling.
    /// </summary>
    [Parameter]
    public bool UseJsHover { get; set; }

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public TooltipHandle<TPayload>? Handle { get; set; }

    [Parameter]
    public TPayload? Payload { get; set; }

    [Parameter]
    public Func<TooltipTriggerState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<TooltipTriggerState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool HasHandle => Handle is not null;

    private bool HasRootContext => RootContext is not null;

    private bool IsDisabled => Disabled || (RootContext?.Disabled ?? false);

    protected override void OnInitialized()
    {
        triggerId = Id ?? Guid.NewGuid().ToIdString();
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        // Handle Id change - unregister old trigger ID
        if (Id is not null && Id != triggerId)
        {
            UnregisterFromCurrentStore(triggerId);
            triggerId = Id;
        }

        // Handle change of handle parameter
        if (Handle != registeredHandle)
        {
            registeredHandle?.UnregisterTrigger(triggerId);
            registeredHandle = Handle;
            Handle?.RegisterTrigger(triggerId, Element, Payload);
        }
        else if (HasHandle)
        {
            // Update payload on existing handle
            Handle!.UpdateTriggerPayload(triggerId, Payload);
        }

        var open = IsOpenedByThisTrigger();
        state = new TooltipTriggerState(open, IsDisabled);

        // Update payload via context if not using handle
        if (!HasHandle)
        {
            RootContext?.SetTriggerPayload(triggerId, Payload);
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            if (HasHandle)
            {
                Handle!.RegisterTrigger(triggerId, Element, Payload);
                registeredHandle = Handle;
            }
            else
            {
                RootContext?.RegisterTriggerElement(triggerId, Element);
            }

            if (UseJsHover && !hoverInitialized && !HasHandle)
            {
                _ = InitializeHoverInteractionAsync();
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null && !HasHandle)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var open = IsOpenedByThisTrigger();
        var popupId = RootContext?.PopupId;

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);

        if (string.IsNullOrEmpty(As) || As == "button")
        {
            builder.AddAttribute(2, "type", "button");
            if (IsDisabled)
            {
                builder.AddAttribute(3, "disabled", true);
            }
        }
        else if (IsDisabled)
        {
            builder.AddAttribute(4, "aria-disabled", "true");
        }

        if (open && popupId is not null)
        {
            builder.AddAttribute(5, "aria-describedby", popupId);
        }

        builder.AddAttribute(6, "id", triggerId);

        if (open)
        {
            builder.AddAttribute(7, "data-popup-open", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(8, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(9, "style", resolvedStyle);
        }

        builder.AddAttribute(10, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnterAsync));
        builder.AddAttribute(11, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseLeaveAsync));
        builder.AddAttribute(12, "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocusAsync));
        builder.AddAttribute(13, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlurAsync));

        if (isComponentRenderAs)
        {
            builder.AddAttribute(14, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(15, component =>
            {
                componentReference = (IReferencableComponent)component;
                var newElement = componentReference.Element;
                if (!Nullable.Equals(Element, newElement))
                {
                    Element = newElement;
                    OnElementChanged();
                }
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddContent(16, ChildContent);
            builder.AddElementReferenceCapture(17, elementReference =>
            {
                if (!Nullable.Equals(Element, elementReference))
                {
                    Element = elementReference;
                    OnElementChanged();
                }
            });
            builder.CloseElement();
        }
    }

    [SlopwatchSuppress("SW003", "Circuit-safe JS interop - intentional empty catch for disconnection during disposal")]
    public async ValueTask DisposeAsync()
    {
        CancelHoverDelay();
        UnregisterFromCurrentStore(triggerId);

        if (UseJsHover && hoverInitialized && moduleTask?.IsValueCreated == true && RootContext is not null)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("disposeHoverInteraction", RootContext.RootId);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    private void OnElementChanged()
    {
        if (HasHandle)
        {
            Handle!.UpdateTriggerElement(triggerId, Element);
        }
        else
        {
            RootContext?.RegisterTriggerElement(triggerId, Element);
        }
    }

    private void UnregisterFromCurrentStore(string id)
    {
        if (registeredHandle is not null)
        {
            registeredHandle.UnregisterTrigger(id);
        }
        else
        {
            RootContext?.UnregisterTriggerElement(id);
        }
    }

    private bool IsOpenedByThisTrigger()
    {
        // Check handle state first if using detached trigger pattern
        if (HasHandle)
        {
            var isOpen = Handle!.IsOpen;
            if (!isOpen)
            {
                return false;
            }
            var activeId = Handle.ActiveTriggerId;
            return activeId == triggerId || (activeId is null && isOpen);
        }

        // Fall back to root context
        if (RootContext is null)
        {
            return false;
        }

        var rootOpen = RootContext.GetOpen();
        if (!rootOpen)
        {
            return false;
        }

        var activeTriggerId = RootContext.ActiveTriggerId;
        return activeTriggerId == triggerId || (activeTriggerId is null && rootOpen);
    }

    private int GetEffectiveDelay()
    {
        if (ProviderContext?.IsInInstantPhase() == true)
        {
            return 0;
        }

        return ProviderContext?.Delay ?? Delay;
    }

    private int GetEffectiveCloseDelay()
    {
        return ProviderContext?.CloseDelay ?? CloseDelay;
    }

    private async Task HandleMouseEnterAsync(MouseEventArgs e)
    {
        // When using JS hover, let JavaScript handle the hover interaction
        if (UseJsHover && hoverInitialized)
        {
            await EventUtilities.InvokeOnMouseEnterAsync(AdditionalAttributes, e);
            return;
        }

        if (IsDisabled || (!HasRootContext && !HasHandle))
        {
            return;
        }

        CancelHoverDelay();

        if (IsOpenedByThisTrigger())
        {
            return;
        }

        var effectiveDelay = GetEffectiveDelay();

        if (effectiveDelay <= 0)
        {
            await RequestOpenAsync(true, TooltipOpenChangeReason.TriggerHover);
            await EventUtilities.InvokeOnMouseEnterAsync(AdditionalAttributes, e);
            return;
        }

        hoverCts = new CancellationTokenSource();
        var token = hoverCts.Token;

        try
        {
            await Task.Delay(effectiveDelay, token);
            if (!token.IsCancellationRequested)
            {
                await RequestOpenAsync(true, TooltipOpenChangeReason.TriggerHover);
            }
        }
        catch (TaskCanceledException)
        {
        }

        await EventUtilities.InvokeOnMouseEnterAsync(AdditionalAttributes, e);
    }

    private async Task HandleMouseLeaveAsync(MouseEventArgs e)
    {
        // When using JS hover, let JavaScript handle the hover interaction
        if (UseJsHover && hoverInitialized)
        {
            await EventUtilities.InvokeOnMouseLeaveAsync(AdditionalAttributes, e);
            return;
        }

        if (IsDisabled || (!HasRootContext && !HasHandle))
        {
            return;
        }

        CancelHoverDelay();

        if (!IsOpenedByThisTrigger())
        {
            return;
        }

        var effectiveCloseDelay = GetEffectiveCloseDelay();

        if (effectiveCloseDelay <= 0)
        {
            await RequestOpenAsync(false, TooltipOpenChangeReason.TriggerHover);
            await EventUtilities.InvokeOnMouseLeaveAsync(AdditionalAttributes, e);
            return;
        }

        hoverCts = new CancellationTokenSource();
        var token = hoverCts.Token;

        try
        {
            await Task.Delay(effectiveCloseDelay, token);
            if (!token.IsCancellationRequested)
            {
                await RequestOpenAsync(false, TooltipOpenChangeReason.TriggerHover);
            }
        }
        catch (TaskCanceledException)
        {
        }

        await EventUtilities.InvokeOnMouseLeaveAsync(AdditionalAttributes, e);
    }

    private async Task HandleFocusAsync(FocusEventArgs e)
    {
        if (IsDisabled || (!HasRootContext && !HasHandle))
        {
            return;
        }

        CancelHoverDelay();

        if (IsOpenedByThisTrigger())
        {
            return;
        }

        await RequestOpenAsync(true, TooltipOpenChangeReason.TriggerFocus);
        await EventUtilities.InvokeOnFocusAsync(AdditionalAttributes, e);
    }

    private async Task HandleBlurAsync(FocusEventArgs e)
    {
        if (IsDisabled || (!HasRootContext && !HasHandle))
        {
            return;
        }

        CancelHoverDelay();

        if (!IsOpenedByThisTrigger())
        {
            return;
        }

        await RequestOpenAsync(false, TooltipOpenChangeReason.TriggerFocus);
        await EventUtilities.InvokeOnBlurAsync(AdditionalAttributes, e);
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

    [SlopwatchSuppress("SW003", "Circuit-safe JS interop - intentional empty catch for disconnection during initialization")]
    private async Task InitializeHoverInteractionAsync()
    {
        if (RootContext is null || !Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await ModuleTask.Value;
            var effectiveDelay = GetEffectiveDelay();
            var effectiveCloseDelay = GetEffectiveCloseDelay();
            var disableHoverablePopup = RootContext.DisableHoverablePopup;
            await module.InvokeVoidAsync("initializeHoverInteraction", RootContext.RootId, Element.Value, effectiveDelay, effectiveCloseDelay, disableHoverablePopup);
            hoverInitialized = true;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private Task RequestOpenAsync(bool open, TooltipOpenChangeReason reason)
    {
        if (HasHandle)
        {
            if (open)
            {
                Handle!.RequestOpen(triggerId, reason);
            }
            else
            {
                Handle!.RequestClose(reason);
            }
            return Task.CompletedTask;
        }

        if (RootContext is not null)
        {
            return RootContext.SetOpenAsync(open, reason, triggerId);
        }

        return Task.CompletedTask;
    }
}

public sealed class TooltipTrigger : TooltipTypedTrigger<object>
{

}