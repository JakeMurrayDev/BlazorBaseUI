using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Dialog;

public sealed class DialogRoot : ComponentBase, IAsyncDisposable, IDialogHandleSubscriber
{
    private readonly string rootId = Guid.NewGuid().ToIdString();
    private readonly Dictionary<string, ElementReference?> triggerElements = new();
    private readonly Dictionary<string, object?> triggerPayloads = new();

    private Lazy<Task<IJSObjectReference>>? moduleTask;
    private bool hasRendered;
    private bool isOpen;
    private bool isMounted;
    private bool nested;
    private bool pendingOpenChange;
    private int nestedDialogCount;
    private string? titleId;
    private string? descriptionId;
    private string? activeTriggerId;
    private ElementReference? popupElement;
    private OpenChangeReason openChangeReason = OpenChangeReason.None;
    private TransitionStatus transitionStatus = TransitionStatus.Undefined;
    private InstantType instantType = InstantType.None;
    private object? payload;
    private DialogRootState state;
    private DialogRootContext context = null!;
    private DotNetObjectReference<DialogRoot>? dotNetRef;
    private IDialogHandle? subscribedHandle;

    private Lazy<Task<IJSObjectReference>> ModuleTask => moduleTask ??= new Lazy<Task<IJSObjectReference>>(() =>
        JSRuntime!.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorBaseUI/blazor-baseui-dialog.js").AsTask());

    [Inject]
    private IJSRuntime? JSRuntime { get; set; }

    [CascadingParameter]
    private DialogRootContext? ParentContext { get; set; }

    [Parameter]
    public bool? Open { get; set; }

    [Parameter]
    public bool DefaultOpen { get; set; }

    [Parameter]
    public ModalMode Modal { get; set; } = ModalMode.True;

    [Parameter]
    public DialogRole Role { get; set; } = DialogRole.Dialog;

    [Parameter]
    public bool DismissOnEscape { get; set; } = true;

    [Parameter]
    public bool DismissOnOutsidePress { get; set; } = true;

    [Parameter]
    public DialogRootActions? ActionsRef { get; set; }

    [Parameter]
    public IDialogHandle? Handle { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public EventCallback<DialogOpenChangeEventArgs> OnOpenChange { get; set; }

    [Parameter]
    public EventCallback<bool> OnOpenChangeComplete { get; set; }

    [Parameter]
    public string? TriggerId { get; set; }

    [Parameter]
    public string? DefaultTriggerId { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public RenderFragment<DialogRootPayloadContext>? ChildContentWithPayload { get; set; }

    private bool IsControlled => Open.HasValue;

    private bool IsTriggerIdControlled => TriggerId is not null;

    private bool CurrentOpen => IsControlled ? Open!.Value : isOpen;

    protected override void OnInitialized()
    {
        isOpen = DefaultOpen;
        isMounted = CurrentOpen;
        nested = ParentContext is not null;
        activeTriggerId = IsTriggerIdControlled ? TriggerId : DefaultTriggerId;
        state = new DialogRootState(CurrentOpen, nestedDialogCount);
        context = CreateContext();

        if (ActionsRef is not null)
        {
            ActionsRef.Unmount = ForceUnmount;
            ActionsRef.Close = () => _ = SetOpenWithExceptionHandlingAsync(false, OpenChangeReason.ImperativeAction);
            ActionsRef.Open = () => _ = SetOpenWithExceptionHandlingAsync(true, OpenChangeReason.ImperativeAction);
            ActionsRef.OpenWithPayload = p => _ = SetOpenWithPayloadWithExceptionHandlingAsync(p, OpenChangeReason.ImperativeAction);
        }

        SubscribeToHandle();
    }

    protected override void OnParametersSet()
    {
        var currentOpen = CurrentOpen;
        if (state.Open != currentOpen)
        {
            state = new DialogRootState(currentOpen, nestedDialogCount);
            if (currentOpen)
            {
                isMounted = true;
                instantType = InstantType.None;
                transitionStatus = TransitionStatus.Starting;
            }
            else
            {
                instantType = InstantType.None;
                transitionStatus = TransitionStatus.Ending;
            }
            context.Open = currentOpen;
            context.Mounted = isMounted;
            context.InstantType = instantType;
            context.TransitionStatus = transitionStatus;
            openChangeReason = OpenChangeReason.None;
            context.OpenChangeReason = openChangeReason;

            // Mark that we need to notify JS after render
            if (hasRendered)
            {
                pendingOpenChange = true;
            }
        }

        if (IsTriggerIdControlled && activeTriggerId != TriggerId)
        {
            activeTriggerId = TriggerId;
            context.ActiveTriggerId = activeTriggerId;
        }

        context.Modal = Modal;
        context.Role = Role;
        context.DismissOnEscape = DismissOnEscape;
        context.DismissOnOutsidePress = DismissOnOutsidePress;

        if (ActionsRef is not null)
        {
            ActionsRef.Unmount = ForceUnmount;
            ActionsRef.Close = () => _ = SetOpenWithExceptionHandlingAsync(false, OpenChangeReason.ImperativeAction);
            ActionsRef.Open = () => _ = SetOpenWithExceptionHandlingAsync(true, OpenChangeReason.ImperativeAction);
            ActionsRef.OpenWithPayload = p => _ = SetOpenWithPayloadWithExceptionHandlingAsync(p, OpenChangeReason.ImperativeAction);
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

            // Handle case where dialog is initially open via controlled prop
            if (CurrentOpen)
            {
                try
                {
                    var module = await ModuleTask.Value;
                    await module.InvokeVoidAsync("setRootOpen", rootId, true);
                }
                catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
                {
                    // Circuit-safe: intentionally empty to prevent crashes during Hot Reload or disconnection
                }
            }
        }
        else if (pendingOpenChange)
        {
            pendingOpenChange = false;
            try
            {
                var module = await ModuleTask.Value;
                await module.InvokeVoidAsync("setRootOpen", rootId, CurrentOpen);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
                // Circuit-safe: intentionally empty to prevent crashes during Hot Reload or disconnection
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<DialogRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);

        if (ChildContentWithPayload is not null)
        {
            var payloadContext = new DialogRootPayloadContext(payload);
            builder.AddComponentParameter(3, "ChildContent", ChildContentWithPayload(payloadContext));
        }
        else
        {
            builder.AddComponentParameter(3, "ChildContent", ChildContent);
        }

        builder.CloseComponent();
    }

    [JSInvokable]
    public async Task OnOutsidePress()
    {
        if (DismissOnOutsidePress)
        {
            await SetOpenAsync(false, OpenChangeReason.OutsidePress);
        }
    }

    [JSInvokable]
    public async Task OnEscapeKey()
    {
        if (DismissOnEscape)
        {
            await SetOpenAsync(false, OpenChangeReason.EscapeKey);
        }
    }

    [JSInvokable]
    public void OnStartingStyleApplied()
    {
        if (transitionStatus == TransitionStatus.Starting)
        {
            transitionStatus = TransitionStatus.Idle;
            context.TransitionStatus = transitionStatus;
            StateHasChanged();
        }
    }

    [JSInvokable]
    public void OnTransitionEnd(bool open)
    {
        transitionStatus = TransitionStatus.Idle;
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

    [JSInvokable]
    public void OnNestedDialogCountChange(int count)
    {
        nestedDialogCount = count;
        context.NestedDialogCount = count;
        state = new DialogRootState(CurrentOpen, count);
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

    void IDialogHandleSubscriber.OnTriggerRegistered(string triggerId, ElementReference? element)
    {
        triggerElements[triggerId] = element;

        if (hasRendered && element.HasValue && activeTriggerId == triggerId)
        {
            _ = SetTriggerElementJsAsync(element.Value);
        }
    }

    void IDialogHandleSubscriber.OnTriggerUnregistered(string triggerId)
    {
        triggerElements.Remove(triggerId);
        triggerPayloads.Remove(triggerId);
    }

    void IDialogHandleSubscriber.OnTriggerElementUpdated(string triggerId, ElementReference? element)
    {
        triggerElements[triggerId] = element;

        if (hasRendered && element.HasValue && activeTriggerId == triggerId)
        {
            _ = SetTriggerElementJsAsync(element.Value);
        }
    }

    void IDialogHandleSubscriber.OnOpenChangeRequested(bool open, OpenChangeReason reason, string? triggerId)
    {
        _ = InvokeAsync(async () =>
        {
            try
            {
                if (open && triggerId is not null)
                {
                    var handlePayload = GetPayloadFromHandle(triggerId);
                    await SetOpenWithTriggerIdAsync(triggerId, handlePayload, reason);
                }
                else
                {
                    await SetOpenAsync(open, reason);
                }
            }
            catch (Exception ex)
            {
                await DispatchExceptionAsync(ex);
            }
        });
    }

    void IDialogHandleSubscriber.OnStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
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

    private DialogRootContext CreateContext() => new(
        rootId: rootId,
        open: CurrentOpen,
        mounted: isMounted,
        nested: nested,
        modal: Modal,
        role: Role,
        dismissOnEscape: DismissOnEscape,
        dismissOnOutsidePress: DismissOnOutsidePress,
        nestedDialogCount: nestedDialogCount,
        openChangeReason: openChangeReason,
        transitionStatus: transitionStatus,
        instantType: instantType,
        titleId: titleId,
        descriptionId: descriptionId,
        activeTriggerId: activeTriggerId,
        payload: payload,
        getOpen: () => CurrentOpen,
        getMounted: () => isMounted,
        getPayload: () => payload,
        getTriggerElement: GetTriggerElement,
        getPopupElement: () => popupElement,
        setTitleId: SetTitleId,
        setDescriptionId: SetDescriptionId,
        registerTriggerElement: RegisterTriggerElement,
        unregisterTriggerElement: UnregisterTriggerElement,
        setPopupElement: SetPopupElement,
        setOpenAsync: SetOpenAsync,
        setOpenWithPayloadAsync: SetOpenWithPayloadAsync,
        setOpenWithTriggerIdAsync: SetOpenWithTriggerIdAsync,
        setTriggerPayload: SetTriggerPayload,
        close: Close,
        forceUnmount: ForceUnmount);

    private async Task InitializeJsAsync()
    {
        try
        {
            var module = await ModuleTask.Value;
            await module.InvokeVoidAsync("initializeRoot", rootId, dotNetRef, Modal.ToDataAttributeString());
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
            // Circuit-safe: intentionally empty to prevent crashes during Hot Reload or disconnection
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
            // Circuit-safe: intentionally empty to prevent crashes during Hot Reload or disconnection
        }
    }

    private async Task SetOpenAsync(bool nextOpen, OpenChangeReason reason)
    {
        if (CurrentOpen == nextOpen)
        {
            return;
        }

        var args = new DialogOpenChangeEventArgs(nextOpen, reason);
        await OnOpenChange.InvokeAsync(args);

        if (args.Canceled)
        {
            return;
        }

        openChangeReason = reason;
        context.OpenChangeReason = reason;

        if (nextOpen)
        {
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
            payload = null;
            context.Payload = null;
        }

        context.InstantType = instantType;
        context.TransitionStatus = transitionStatus;
        context.Mounted = isMounted;

        if (!IsControlled)
        {
            isOpen = nextOpen;
        }

        state = new DialogRootState(nextOpen, nestedDialogCount);
        context.Open = nextOpen;

        SyncHandleState(nextOpen, activeTriggerId);

        if (hasRendered)
        {
            // Clear pendingOpenChange since we're handling the state change directly
            pendingOpenChange = false;

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

    private async Task SetOpenWithPayloadAsync(object? newPayload, OpenChangeReason reason)
    {
        payload = newPayload;
        context.Payload = newPayload;
        await SetOpenAsync(true, reason);
    }

    private async Task SetOpenWithTriggerIdAsync(string? triggerId, object? newPayload, OpenChangeReason reason)
    {
        if (!IsTriggerIdControlled)
        {
            activeTriggerId = triggerId;
            context.ActiveTriggerId = activeTriggerId;
        }

        // Try to get payload from handle first, then use provided payload
        if (newPayload is null && triggerId is not null)
        {
            var handlePayload = GetPayloadFromHandle(triggerId);
            if (handlePayload is not null)
            {
                newPayload = handlePayload;
            }
            else if (triggerPayloads.TryGetValue(triggerId, out var storedPayload))
            {
                newPayload = storedPayload;
            }
        }

        payload = newPayload;
        context.Payload = newPayload;
        await SetOpenAsync(true, reason);
    }

    private async Task SetOpenWithExceptionHandlingAsync(bool nextOpen, OpenChangeReason reason)
    {
        try
        {
            await SetOpenAsync(nextOpen, reason);
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private async Task SetOpenWithPayloadWithExceptionHandlingAsync(object? newPayload, OpenChangeReason reason)
    {
        try
        {
            await SetOpenWithPayloadAsync(newPayload, reason);
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
    }

    private void Close()
    {
        _ = SetOpenWithExceptionHandlingAsync(false, OpenChangeReason.ClosePress);
    }

    private void ForceUnmount()
    {
        isMounted = false;
        context.Mounted = false;
        transitionStatus = TransitionStatus.Undefined;
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

public readonly record struct DialogRootPayloadContext(object? Payload);
