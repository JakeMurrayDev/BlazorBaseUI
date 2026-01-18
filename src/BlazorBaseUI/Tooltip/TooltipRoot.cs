using BlazorBaseUI.Popover;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Tooltip;

public sealed class TooltipRoot : ComponentBase, IAsyncDisposable, ITooltipHandleSubscriber
{
    private readonly string rootId = Guid.NewGuid().ToIdString();
    private readonly string popupId = Guid.NewGuid().ToIdString();
    private readonly Dictionary<string, ElementReference?> triggerElements = new();
    private readonly Dictionary<string, object?> triggerPayloads = new();

    private Lazy<Task<IJSObjectReference>>? moduleTask;
    private bool hasRendered;
    private bool isOpen;
    private bool isMounted;
    private string? activeTriggerId;
    private object? payload;
    private ElementReference? positionerElement;
    private ElementReference? popupElement;
    private TooltipOpenChangeReason openChangeReason = TooltipOpenChangeReason.None;
    private Popover.TransitionStatus transitionStatus = Popover.TransitionStatus.None;
    private TooltipInstantType instantType = TooltipInstantType.None;
    private TooltipRootState state;
    private TooltipRootContext context = null!;
    private DotNetObjectReference<TooltipRoot>? dotNetRef;
    private ITooltipHandle? subscribedHandle;

    private Lazy<Task<IJSObjectReference>> ModuleTask => moduleTask ??= new Lazy<Task<IJSObjectReference>>(() =>
        JSRuntime!.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorBaseUI/blazor-baseui-tooltip.js").AsTask());

    [Inject]
    private IJSRuntime? JSRuntime { get; set; }

    [CascadingParameter]
    private TooltipProviderContext? ProviderContext { get; set; }

    [Parameter]
    public bool? Open { get; set; }

    [Parameter]
    public bool DefaultOpen { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool DisableHoverablePopup { get; set; }

    [Parameter]
    public TrackCursorAxis TrackCursorAxis { get; set; } = TrackCursorAxis.None;

    [Parameter]
    public string? TriggerId { get; set; }

    [Parameter]
    public string? DefaultTriggerId { get; set; }

    [Parameter]
    public TooltipRootActions? ActionsRef { get; set; }

    [Parameter]
    public ITooltipHandle? Handle { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public EventCallback<TooltipOpenChangeEventArgs> OnOpenChange { get; set; }

    [Parameter]
    public EventCallback<bool> OnOpenChangeComplete { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment<TooltipRootPayloadContext>? ChildContentWithPayload { get; set; }

    private bool IsControlled => Open.HasValue;

    private bool CurrentOpen => IsControlled ? Open!.Value : isOpen;

    protected override void OnInitialized()
    {
        isOpen = DefaultOpen;
        isMounted = CurrentOpen;
        activeTriggerId = DefaultTriggerId;
        state = new TooltipRootState(CurrentOpen);
        context = CreateContext();

        if (ActionsRef is not null)
        {
            ActionsRef.Unmount = ForceUnmount;
            ActionsRef.Close = () => _ = SetOpenWithExceptionHandlingAsync(false, TooltipOpenChangeReason.ImperativeAction, null);
            ActionsRef.Open = () => _ = SetOpenWithExceptionHandlingAsync(true, TooltipOpenChangeReason.ImperativeAction, null);
            ActionsRef.OpenWithTriggerId = triggerId => _ = SetOpenWithExceptionHandlingAsync(true, TooltipOpenChangeReason.ImperativeAction, triggerId);
        }

        SubscribeToHandle();
    }

    protected override void OnParametersSet()
    {
        var currentOpen = CurrentOpen;
        if (state.Open != currentOpen)
        {
            state = new TooltipRootState(currentOpen);
            if (currentOpen)
            {
                isMounted = true;
            }
            context.Open = currentOpen;
            context.Mounted = isMounted;
        }

        if (TriggerId is not null)
        {
            activeTriggerId = TriggerId;
            context.ActiveTriggerId = activeTriggerId;
        }

        context.Disabled = Disabled;
        context.DisableHoverablePopup = DisableHoverablePopup;
        context.TrackCursorAxis = TrackCursorAxis;

        if (ActionsRef is not null)
        {
            ActionsRef.Unmount = ForceUnmount;
            ActionsRef.Close = () => _ = SetOpenWithExceptionHandlingAsync(false, TooltipOpenChangeReason.ImperativeAction, null);
            ActionsRef.Open = () => _ = SetOpenWithExceptionHandlingAsync(true, TooltipOpenChangeReason.ImperativeAction, null);
            ActionsRef.OpenWithTriggerId = triggerId => _ = SetOpenWithExceptionHandlingAsync(true, TooltipOpenChangeReason.ImperativeAction, triggerId);
        }

        // Handle change of handle parameter
        if (Handle != subscribedHandle)
        {
            UnsubscribeFromHandle();
            SubscribeToHandle();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            dotNetRef = DotNetObjectReference.Create(this);
            await InitializeJsAsync();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<TooltipRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);

        if (ChildContentWithPayload is not null)
        {
            var payloadContext = new TooltipRootPayloadContext(payload);
            builder.AddComponentParameter(3, "ChildContent", ChildContentWithPayload(payloadContext));
        }
        else
        {
            builder.AddComponentParameter(3, "ChildContent", ChildContent);
        }

        builder.CloseComponent();
    }

    [JSInvokable]
    public async Task OnEscapeKey()
    {
        await SetOpenAsync(false, TooltipOpenChangeReason.EscapeKey, null);
    }

    [JSInvokable]
    public void OnStartingStyleApplied()
    {
        if (transitionStatus == Popover.TransitionStatus.Starting)
        {
            transitionStatus = Popover.TransitionStatus.None;
            context.TransitionStatus = transitionStatus;
            StateHasChanged();
        }
    }

    [JSInvokable]
    public void OnTransitionEnd(bool open)
    {
        transitionStatus = Popover.TransitionStatus.None;
        context.TransitionStatus = transitionStatus;

        if (!open)
        {
            isMounted = false;
            context.Mounted = false;
            activeTriggerId = null;
            context.ActiveTriggerId = null;
            payload = null;
            context.Payload = null;
        }

        instantType = TooltipInstantType.None;
        context.InstantType = instantType;

        _ = InvokeOpenChangeCompleteAsync(open);
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        UnsubscribeFromHandle();

        if (moduleTask?.IsValueCreated == true && hasRendered)
        {
            try
            {
                var module = await ModuleTask.Value;
                await module.InvokeVoidAsync("disposeRoot", rootId);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
                // Circuit-safe: intentionally empty to prevent crashes during Hot Reload or disconnection
            }
        }

        dotNetRef?.Dispose();
    }

    void ITooltipHandleSubscriber.OnTriggerRegistered(string triggerId, ElementReference? element)
    {
        triggerElements[triggerId] = element;

        if (hasRendered && element.HasValue && activeTriggerId == triggerId)
        {
            _ = SetTriggerElementJsAsync(element.Value);
        }
    }

    void ITooltipHandleSubscriber.OnTriggerUnregistered(string triggerId)
    {
        triggerElements.Remove(triggerId);
        triggerPayloads.Remove(triggerId);
    }

    void ITooltipHandleSubscriber.OnTriggerElementUpdated(string triggerId, ElementReference? element)
    {
        triggerElements[triggerId] = element;

        if (hasRendered && element.HasValue && activeTriggerId == triggerId)
        {
            _ = SetTriggerElementJsAsync(element.Value);
        }
    }

    void ITooltipHandleSubscriber.OnOpenChangeRequested(bool open, TooltipOpenChangeReason reason, string? triggerId)
    {
        _ = InvokeAsync(async () =>
        {
            try
            {
                await SetOpenAsync(open, reason, triggerId);
            }
            catch (Exception ex)
            {
                await DispatchExceptionAsync(ex);
            }
        });
    }

    void ITooltipHandleSubscriber.OnStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    internal async Task SetOpenAsync(bool nextOpen, TooltipOpenChangeReason reason, string? triggerId)
    {
        if (CurrentOpen == nextOpen)
        {
            return;
        }

        if (Disabled && nextOpen)
        {
            return;
        }

        var args = new TooltipOpenChangeEventArgs(nextOpen, reason);
        await OnOpenChange.InvokeAsync(args);

        if (args.Canceled)
        {
            return;
        }

        openChangeReason = reason;
        context.OpenChangeReason = reason;

        if (nextOpen)
        {
            if (triggerId is not null)
            {
                activeTriggerId = triggerId;
                context.ActiveTriggerId = triggerId;

                // Try to get payload from handle first, then from local storage
                var handlePayload = GetPayloadFromHandle(triggerId);
                if (handlePayload is not null)
                {
                    payload = handlePayload;
                }
                else if (triggerPayloads.TryGetValue(triggerId, out var triggerPayload))
                {
                    payload = triggerPayload;
                }
                context.Payload = payload;
            }

            instantType = reason == TooltipOpenChangeReason.TriggerFocus
                ? TooltipInstantType.Focus
                : (ProviderContext?.IsInInstantPhase() == true ? TooltipInstantType.Delay : TooltipInstantType.None);
            transitionStatus = Popover.TransitionStatus.Starting;
            isMounted = true;
        }
        else
        {
            instantType = reason switch
            {
                TooltipOpenChangeReason.EscapeKey or TooltipOpenChangeReason.OutsidePress => TooltipInstantType.Dismiss,
                _ => TooltipInstantType.None
            };
            transitionStatus = Popover.TransitionStatus.Ending;

            ProviderContext?.SetLastClosedTime();
        }

        context.InstantType = instantType;
        context.TransitionStatus = transitionStatus;
        context.Mounted = isMounted;

        if (!IsControlled)
        {
            isOpen = nextOpen;
        }

        state = new TooltipRootState(nextOpen);
        context.Open = nextOpen;

        SyncHandleState(nextOpen, activeTriggerId);

        if (hasRendered)
        {
            try
            {
                var module = await ModuleTask.Value;
                await module.InvokeVoidAsync("setRootOpen", rootId, nextOpen);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
                // Circuit-safe: intentionally empty to prevent crashes during Hot Reload or disconnection
            }
        }

        await OpenChanged.InvokeAsync(nextOpen);
        StateHasChanged();
    }

    internal void RegisterTriggerElement(string triggerId, ElementReference? element)
    {
        triggerElements[triggerId] = element;

        if (hasRendered && element.HasValue && activeTriggerId == triggerId)
        {
            _ = SetTriggerElementJsAsync(element.Value);
        }
    }

    internal void UnregisterTriggerElement(string triggerId)
    {
        triggerElements.Remove(triggerId);
        triggerPayloads.Remove(triggerId);
    }

    internal void SetTriggerPayload(string triggerId, object? triggerPayload)
    {
        triggerPayloads[triggerId] = triggerPayload;

        if (activeTriggerId == triggerId && CurrentOpen)
        {
            payload = triggerPayload;
            context.Payload = payload;
        }
    }

    internal void SetPositionerElement(ElementReference? element)
    {
        positionerElement = element;
    }

    internal void SetPopupElement(ElementReference? element)
    {
        popupElement = element;

        if (hasRendered && element.HasValue)
        {
            _ = SetPopupElementJsAsync(element.Value);
        }
    }

    internal bool GetOpen() => CurrentOpen;

    internal bool GetMounted() => isMounted;

    internal object? GetPayload() => payload;

    internal ElementReference? GetTriggerElement()
    {
        if (activeTriggerId is not null && triggerElements.TryGetValue(activeTriggerId, out var element))
        {
            return element;
        }

        // Try to get from handle if available
        if (Handle is not null && activeTriggerId is not null)
        {
            var handleElement = GetTriggerElementFromHandle(activeTriggerId);
            if (handleElement.HasValue)
            {
                return handleElement;
            }
        }

        return triggerElements.Values.FirstOrDefault();
    }

    private TooltipRootContext CreateContext() => new(
        rootId: rootId,
        popupId: popupId,
        open: CurrentOpen,
        mounted: isMounted,
        disabled: Disabled,
        openChangeReason: openChangeReason,
        transitionStatus: transitionStatus,
        instantType: instantType,
        trackCursorAxis: TrackCursorAxis,
        disableHoverablePopup: DisableHoverablePopup,
        activeTriggerId: activeTriggerId,
        payload: payload,
        getOpen: () => CurrentOpen,
        getMounted: () => isMounted,
        getPayload: () => payload,
        getTriggerElement: GetTriggerElement,
        registerTriggerElement: RegisterTriggerElement,
        unregisterTriggerElement: UnregisterTriggerElement,
        setPositionerElement: SetPositionerElement,
        setPopupElement: SetPopupElement,
        setOpenAsync: SetOpenAsync,
        setTriggerPayload: SetTriggerPayload,
        forceUnmount: ForceUnmount);

    private async Task InitializeJsAsync()
    {
        try
        {
            var module = await ModuleTask.Value;
            await module.InvokeVoidAsync("initializeRoot", rootId, dotNetRef);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
            // Circuit-safe: intentionally empty to prevent crashes during Hot Reload or disconnection
        }
    }

    private async Task SetTriggerElementJsAsync(ElementReference element)
    {
        try
        {
            var module = await ModuleTask.Value;
            await module.InvokeVoidAsync("setTriggerElement", rootId, element);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
            // Circuit-safe: intentionally empty to prevent crashes during Hot Reload or disconnection
        }
    }

    private async Task SetPopupElementJsAsync(ElementReference element)
    {
        try
        {
            var module = await ModuleTask.Value;
            await module.InvokeVoidAsync("setPopupElement", rootId, element);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
            // Circuit-safe: intentionally empty to prevent crashes during Hot Reload or disconnection
        }
    }

    private async Task SetOpenWithExceptionHandlingAsync(bool nextOpen, TooltipOpenChangeReason reason, string? triggerId)
    {
        try
        {
            await SetOpenAsync(nextOpen, reason, triggerId);
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private void ForceUnmount()
    {
        isMounted = false;
        context.Mounted = false;
        transitionStatus = Popover.TransitionStatus.None;
        context.TransitionStatus = transitionStatus;
        activeTriggerId = null;
        context.ActiveTriggerId = null;
        payload = null;
        context.Payload = null;
        _ = InvokeOpenChangeCompleteAsync(false);
        StateHasChanged();
    }

    private async Task InvokeOpenChangeCompleteAsync(bool open)
    {
        try
        {
            await OnOpenChangeComplete.InvokeAsync(open);
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private void SubscribeToHandle()
    {
        if (Handle is null)
        {
            return;
        }

        subscribedHandle = Handle;
        Handle.Subscribe(this);
    }

    private void UnsubscribeFromHandle()
    {
        if (subscribedHandle is null)
        {
            return;
        }

        subscribedHandle.Unsubscribe(this);
        subscribedHandle = null;
    }

    private void SyncHandleState(bool open, string? triggerId)
    {
        Handle?.SyncState(open, triggerId, payload);
    }

    private object? GetPayloadFromHandle(string triggerId)
    {
        return Handle?.GetTriggerPayloadAsObject(triggerId);
    }

    private ElementReference? GetTriggerElementFromHandle(string triggerId)
    {
        return Handle?.GetTriggerElement(triggerId);
    }
}

public sealed class TooltipRootActions
{
    public Action? Unmount { get; internal set; }

    public Action? Close { get; internal set; }

    public Action? Open { get; internal set; }

    public Action<string>? OpenWithTriggerId { get; internal set; }
}

public readonly record struct TooltipRootPayloadContext(object? Payload);
