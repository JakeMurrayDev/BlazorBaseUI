using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Popover;

public sealed class PopoverRoot : ComponentBase, IAsyncDisposable, IPopoverHandleSubscriber
{
    private readonly string rootId = Guid.NewGuid().ToIdString();
    private readonly Dictionary<string, ElementReference?> triggerElements = [];

    private Lazy<Task<IJSObjectReference>>? moduleTask;
    private bool hasRendered;
    private bool isOpen;
    private bool isMounted;
    private string? titleId;
    private string? descriptionId;
    private string? activeTriggerId;
    private ElementReference? triggerElement;
    private ElementReference? positionerElement;
    private ElementReference? popupElement;
    private OpenChangeReason openChangeReason = OpenChangeReason.None;
    private TransitionStatus transitionStatus = TransitionStatus.None;
    private InstantType instantType = InstantType.None;
    private object? payload;
    private PopoverRootState state;
    private PopoverRootContext context = null!;
    private DotNetObjectReference<PopoverRoot>? dotNetRef;
    private IPopoverHandle? subscribedHandle;

    private Lazy<Task<IJSObjectReference>> ModuleTask => moduleTask ??= new Lazy<Task<IJSObjectReference>>(() =>
        JSRuntime!.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorBaseUI/blazor-baseui-popover.js").AsTask());

    [Inject]
    private IJSRuntime? JSRuntime { get; set; }

    [Parameter]
    public bool? Open { get; set; }

    [Parameter]
    public bool DefaultOpen { get; set; }

    [Parameter]
    public ModalMode Modal { get; set; } = ModalMode.False;

    [Parameter]
    public string? TriggerId { get; set; }

    [Parameter]
    public string? DefaultTriggerId { get; set; }

    [Parameter]
    public PopoverRootActions? ActionsRef { get; set; }

    [Parameter]
    public IPopoverHandle? Handle { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public EventCallback<PopoverOpenChangeEventArgs> OnOpenChange { get; set; }

    [Parameter]
    public EventCallback<bool> OnOpenChangeComplete { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment<PopoverRootPayloadContext>? ChildContentWithPayload { get; set; }

    private bool IsControlled => Open.HasValue;

    private bool CurrentOpen => IsControlled ? Open!.Value : isOpen;

    private string? CurrentTriggerId => TriggerId ?? activeTriggerId;

    protected override void OnInitialized()
    {
        isOpen = DefaultOpen;
        isMounted = CurrentOpen;
        activeTriggerId = DefaultTriggerId;
        state = new PopoverRootState(CurrentOpen);
        context = CreateContext();

        if (ActionsRef is not null)
        {
            ActionsRef.Unmount = ForceUnmount;
            ActionsRef.Close = () => _ = SetOpenWithExceptionHandlingAsync(false, OpenChangeReason.ImperativeAction, null);
        }

        SubscribeToHandle();
    }

    protected override void OnParametersSet()
    {
        var currentOpen = CurrentOpen;
        if (state.Open != currentOpen)
        {
            state = new PopoverRootState(currentOpen);
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

        context.Modal = Modal;

        if (ActionsRef is not null)
        {
            ActionsRef.Unmount = ForceUnmount;
            ActionsRef.Close = () => _ = SetOpenWithExceptionHandlingAsync(false, OpenChangeReason.ImperativeAction, null);
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
        builder.OpenComponent<CascadingValue<PopoverRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);

        if (ChildContentWithPayload is not null)
        {
            var payloadContext = new PopoverRootPayloadContext(payload);
            builder.AddComponentParameter(3, "ChildContent", ChildContentWithPayload(payloadContext));
        }
        else
        {
            builder.AddComponentParameter(3, "ChildContent", ChildContent);
        }

        builder.CloseComponent();
    }

    private PopoverRootContext CreateContext() => new(
        rootId: rootId,
        open: CurrentOpen,
        mounted: isMounted,
        modal: Modal,
        openChangeReason: openChangeReason,
        transitionStatus: transitionStatus,
        instantType: instantType,
        titleId: titleId,
        descriptionId: descriptionId,
        activeTriggerId: activeTriggerId,
        payload: payload,
        getOpen: () => CurrentOpen,
        getMounted: () => isMounted,
        getTriggerElement: GetTriggerElement,
        getPositionerElement: () => positionerElement,
        getPopupElement: () => popupElement,
        setTitleId: SetTitleId,
        setDescriptionId: SetDescriptionId,
        setTriggerElement: SetTriggerElement,
        setPositionerElement: SetPositionerElement,
        setPopupElement: SetPopupElement,
        setOpenAsync: SetOpenAsyncFromContext,
        close: Close,
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
        }
    }

    private void SetTitleId(string id)
    {
        titleId = id;
        context.TitleId = id;
    }

    private void SetDescriptionId(string id)
    {
        descriptionId = id;
        context.DescriptionId = id;
    }

    private void SetTriggerElement(ElementReference? element)
    {
        triggerElement = element;

        if (hasRendered && element.HasValue)
        {
            _ = SetTriggerElementAsync(element.Value);
        }
    }

    private async Task SetTriggerElementAsync(ElementReference element)
    {
        try
        {
            var module = await ModuleTask.Value;
            await module.InvokeVoidAsync("setTriggerElement", rootId, element);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private void SetPositionerElement(ElementReference? element)
    {
        positionerElement = element;
    }

    private void SetPopupElement(ElementReference? element)
    {
        popupElement = element;

        if (hasRendered && element.HasValue)
        {
            _ = SetPopupElementAsync(element.Value);
        }
    }

    private async Task SetPopupElementAsync(ElementReference element)
    {
        try
        {
            var module = await ModuleTask.Value;
            await module.InvokeVoidAsync("setPopupElement", rootId, element);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private Task SetOpenAsyncFromContext(bool nextOpen, OpenChangeReason reason, object? payloadFromTrigger)
    {
        // Context calls this with payload from trigger - we ignore the payload here
        // and let triggers pass their payload via the RequestOpenAsync path
        return SetOpenAsync(nextOpen, reason, (string?)null);
    }

    private void Close()
    {
        _ = SetOpenAsync(false, OpenChangeReason.ClosePress, null);
    }

    private void ForceUnmount()
    {
        isMounted = false;
        context.Mounted = false;
        transitionStatus = TransitionStatus.None;
        context.TransitionStatus = transitionStatus;
        activeTriggerId = null;
        context.ActiveTriggerId = null;
        payload = null;
        context.Payload = null;
        _ = InvokeOpenChangeCompleteAsync(false);
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnOutsidePress()
    {
        await SetOpenAsync(false, OpenChangeReason.OutsidePress, null);
    }

    [JSInvokable]
    public async Task OnEscapeKey()
    {
        await SetOpenAsync(false, OpenChangeReason.EscapeKey, null);
    }

    [JSInvokable]
    public void OnStartingStyleApplied()
    {
        if (transitionStatus == TransitionStatus.Starting)
        {
            transitionStatus = TransitionStatus.None;
            context.TransitionStatus = transitionStatus;
            StateHasChanged();
        }
    }

    [JSInvokable]
    public async Task OnHoverOpen()
    {
        await SetOpenAsync(true, OpenChangeReason.TriggerHover, null);
    }

    [JSInvokable]
    public async Task OnHoverClose()
    {
        await SetOpenAsync(false, OpenChangeReason.TriggerHover, null);
    }

    [JSInvokable]
    public void OnTransitionEnd(bool open)
    {
        transitionStatus = TransitionStatus.None;
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

        instantType = InstantType.None;
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
            }
        }

        dotNetRef?.Dispose();
    }

    internal async Task SetOpenAsync(bool nextOpen, OpenChangeReason reason, string? triggerId)
    {
        if (CurrentOpen == nextOpen)
        {
            return;
        }

        var args = new PopoverOpenChangeEventArgs(nextOpen, reason);
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

                var handlePayload = GetPayloadFromHandle(triggerId);
                if (handlePayload is not null)
                {
                    payload = handlePayload;
                }
                context.Payload = payload;
            }

            instantType = reason == OpenChangeReason.TriggerPress ? InstantType.Click : InstantType.None;
            transitionStatus = TransitionStatus.Starting;
            isMounted = true;
        }
        else
        {
            instantType = reason switch
            {
                OpenChangeReason.EscapeKey or OpenChangeReason.OutsidePress => InstantType.Dismiss,
                OpenChangeReason.TriggerPress or OpenChangeReason.ClosePress => InstantType.Click,
                _ => InstantType.None
            };
            transitionStatus = TransitionStatus.Ending;
        }

        context.InstantType = instantType;
        context.TransitionStatus = transitionStatus;
        context.Mounted = isMounted;

        if (!IsControlled)
        {
            isOpen = nextOpen;
        }

        state = new PopoverRootState(nextOpen);
        context.Open = nextOpen;

        SyncHandleState(nextOpen, activeTriggerId);

        if (hasRendered)
        {
            try
            {
                var module = await ModuleTask.Value;
                var reasonString = reason == OpenChangeReason.TriggerHover ? "trigger-hover" : null;
                await module.InvokeVoidAsync("setRootOpen", rootId, nextOpen, reasonString);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }

        await OpenChanged.InvokeAsync(nextOpen);
        StateHasChanged();
    }

    void IPopoverHandleSubscriber.OnTriggerRegistered(string triggerId, ElementReference? element)
    {
        triggerElements[triggerId] = element;

        if (hasRendered && element.HasValue && activeTriggerId == triggerId)
        {
            _ = SetTriggerElementAsync(element.Value);
        }
    }

    void IPopoverHandleSubscriber.OnTriggerUnregistered(string triggerId)
    {
        triggerElements.Remove(triggerId);
    }

    void IPopoverHandleSubscriber.OnTriggerElementUpdated(string triggerId, ElementReference? element)
    {
        triggerElements[triggerId] = element;

        if (hasRendered && element.HasValue && activeTriggerId == triggerId)
        {
            _ = SetTriggerElementAsync(element.Value);
        }
    }

    void IPopoverHandleSubscriber.OnOpenChangeRequested(bool open, OpenChangeReason reason, string? triggerId)
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

    void IPopoverHandleSubscriber.OnStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task SetOpenWithExceptionHandlingAsync(bool nextOpen, OpenChangeReason reason, string? triggerId)
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

    private ElementReference? GetTriggerElement()
    {
        // First try the local triggerElement (for nested triggers)
        if (triggerElement.HasValue)
        {
            return triggerElement;
        }

        // Try to get from local storage based on active trigger
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

        // Fall back to first available trigger element
        return triggerElements.Values.FirstOrDefault();
    }
}

public sealed class PopoverRootActions
{
    public Action? Unmount { get; internal set; }

    public Action? Close { get; internal set; }
}

public readonly record struct PopoverRootPayloadContext(object? Payload);
