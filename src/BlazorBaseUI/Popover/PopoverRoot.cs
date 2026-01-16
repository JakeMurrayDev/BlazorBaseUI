using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Popover;

public sealed class PopoverRoot : ComponentBase, IAsyncDisposable
{
    private readonly string rootId = Guid.NewGuid().ToIdString();

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
    private PopoverRootState state;
    private PopoverRootContext context = null!;
    private DotNetObjectReference<PopoverRoot>? dotNetRef;

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
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public EventCallback<PopoverOpenChangeEventArgs> OnOpenChange { get; set; }

    [Parameter]
    public EventCallback<bool> OnOpenChangeComplete { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

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
            ActionsRef.Close = () => _ = SetOpenAsync(false, OpenChangeReason.ImperativeAction);
        }
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
            ActionsRef.Close = () => _ = SetOpenAsync(false, OpenChangeReason.ImperativeAction);
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
        builder.AddComponentParameter(3, "ChildContent", ChildContent);
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
        getOpen: () => CurrentOpen,
        getMounted: () => isMounted,
        getTriggerElement: () => triggerElement,
        getPositionerElement: () => positionerElement,
        getPopupElement: () => popupElement,
        setTitleId: SetTitleId,
        setDescriptionId: SetDescriptionId,
        setTriggerElement: SetTriggerElement,
        setPositionerElement: SetPositionerElement,
        setPopupElement: SetPopupElement,
        setOpenAsync: SetOpenAsync,
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

    private async void SetTriggerElement(ElementReference? element)
    {
        triggerElement = element;

        if (hasRendered && element.HasValue)
        {
            try
            {
                var module = await ModuleTask.Value;
                await module.InvokeVoidAsync("setTriggerElement", rootId, element.Value);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    private void SetPositionerElement(ElementReference? element)
    {
        positionerElement = element;
    }

    private async void SetPopupElement(ElementReference? element)
    {
        popupElement = element;

        if (hasRendered && element.HasValue)
        {
            try
            {
                var module = await ModuleTask.Value;
                await module.InvokeVoidAsync("setPopupElement", rootId, element.Value);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    private async Task SetOpenAsync(bool nextOpen, OpenChangeReason reason)
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

        // Determine instant type based on reason
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

        if (hasRendered)
        {
            try
            {
                var module = await ModuleTask.Value;
                await module.InvokeVoidAsync("setRootOpen", rootId, nextOpen);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }

        await OpenChanged.InvokeAsync(nextOpen);
        StateHasChanged();
    }

    private void Close()
    {
        _ = SetOpenAsync(false, OpenChangeReason.ClosePress);
    }

    private void ForceUnmount()
    {
        isMounted = false;
        context.Mounted = false;
        transitionStatus = TransitionStatus.None;
        context.TransitionStatus = transitionStatus;
        activeTriggerId = null;
        context.ActiveTriggerId = null;
        _ = OnOpenChangeComplete.InvokeAsync(false);
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnOutsidePress()
    {
        await SetOpenAsync(false, OpenChangeReason.OutsidePress);
    }

    [JSInvokable]
    public async Task OnEscapeKey()
    {
        await SetOpenAsync(false, OpenChangeReason.EscapeKey);
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
        }

        instantType = InstantType.None;
        context.InstantType = instantType;

        _ = OnOpenChangeComplete.InvokeAsync(open);
        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
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
}

public sealed class PopoverRootActions
{
    public Action? Unmount { get; internal set; }

    public Action? Close { get; internal set; }
}
