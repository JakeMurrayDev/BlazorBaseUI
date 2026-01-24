using BlazorBaseUI.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Menu;

public sealed class MenuRoot : ComponentBase, IAsyncDisposable
{
    private readonly string rootId = Guid.NewGuid().ToIdString();

    private Lazy<Task<IJSObjectReference>>? moduleTask;
    private bool hasRendered;
    private bool isOpen;
    private bool isMounted;
    private int activeIndex = -1;
    private ElementReference? triggerElement;
    private ElementReference? positionerElement;
    private ElementReference? popupElement;
    private OpenChangeReason openChangeReason = OpenChangeReason.None;
    private TransitionStatus transitionStatus = TransitionStatus.None;
    private InstantType instantType = InstantType.None;
    private MenuRootState state;
    private MenuRootContext context = null!;
    private DotNetObjectReference<MenuRoot>? dotNetRef;

    private Lazy<Task<IJSObjectReference>> ModuleTask => moduleTask ??= new Lazy<Task<IJSObjectReference>>(() =>
        JSRuntime!.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorBaseUI/blazor-baseui-menu.js").AsTask());

    private bool IsControlled => Open.HasValue;

    private bool CurrentOpen => IsControlled ? Open!.Value : isOpen;

    private MenuParentType ParentType => ParentMenuContext is not null ? MenuParentType.Menu : MenuParentType.None;

    [Inject]
    private IJSRuntime? JSRuntime { get; set; }

    [CascadingParameter]
    private MenuRootContext? ParentMenuContext { get; set; }

    [Parameter]
    public bool? Open { get; set; }

    [Parameter]
    public bool DefaultOpen { get; set; }

    [Parameter]
    public ModalMode Modal { get; set; } = ModalMode.True;

    [Parameter]
    public bool LoopFocus { get; set; } = true;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool CloseParentOnEsc { get; set; }

    [Parameter]
    public MenuRootActions? ActionsRef { get; set; }

    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    [Parameter]
    public EventCallback<MenuOpenChangeEventArgs> OnOpenChange { get; set; }

    [Parameter]
    public EventCallback<bool> OnOpenChangeComplete { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnInitialized()
    {
        isOpen = DefaultOpen;
        isMounted = CurrentOpen;
        state = new MenuRootState(CurrentOpen);
        context = CreateContext();

        if (ActionsRef is not null)
        {
            ActionsRef.Unmount = ForceUnmount;
            ActionsRef.Close = () => _ = SetOpenWithExceptionHandlingAsync(false, OpenChangeReason.ImperativeAction);
        }
    }

    protected override void OnParametersSet()
    {
        var currentOpen = CurrentOpen;
        if (state.Open != currentOpen)
        {
            state = new MenuRootState(currentOpen);
            if (currentOpen)
            {
                isMounted = true;
            }
            context.Open = currentOpen;
            context.Mounted = isMounted;
        }

        context.Disabled = Disabled;

        if (ActionsRef is not null)
        {
            ActionsRef.Unmount = ForceUnmount;
            ActionsRef.Close = () => _ = SetOpenWithExceptionHandlingAsync(false, OpenChangeReason.ImperativeAction);
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            dotNetRef = DotNetObjectReference.Create(this);
            _ = InitializeJsAsync();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<MenuRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask?.IsValueCreated == true && hasRendered)
        {
            try
            {
                var module = await ModuleTask.Value;
                await module.InvokeVoidAsync("disposeRoot", rootId);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
                // Circuit-safe JS interop - intentional empty catch for disconnection during disposal
            }
        }

        dotNetRef?.Dispose();
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
            activeIndex = -1;
            context.ActiveIndex = activeIndex;
        }

        instantType = InstantType.None;
        context.InstantType = instantType;

        _ = InvokeOpenChangeCompleteAsync(open);
        StateHasChanged();
    }

    [JSInvokable]
    public void OnActiveIndexChange(int index)
    {
        if (activeIndex != index)
        {
            activeIndex = index;
            context.ActiveIndex = index;
            StateHasChanged();
        }
    }

    [JSInvokable]
    public async Task OnHoverOpen()
    {
        await SetOpenAsync(true, OpenChangeReason.TriggerHover);
    }

    [JSInvokable]
    public async Task OnHoverClose()
    {
        await SetOpenAsync(false, OpenChangeReason.TriggerHover);
    }

    internal async Task SetOpenAsync(bool nextOpen, OpenChangeReason reason)
    {
        if (CurrentOpen == nextOpen)
        {
            return;
        }

        var args = new MenuOpenChangeEventArgs(nextOpen, reason);
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
            activeIndex = -1;
        }
        else
        {
            instantType = reason switch
            {
                OpenChangeReason.EscapeKey or OpenChangeReason.OutsidePress => InstantType.Dismiss,
                OpenChangeReason.TriggerPress or OpenChangeReason.ClosePress or OpenChangeReason.ItemPress => InstantType.Click,
                OpenChangeReason.ListNavigation or OpenChangeReason.TriggerFocus or OpenChangeReason.TriggerHover or OpenChangeReason.SiblingOpen => InstantType.Group,
                _ => InstantType.None
            };
            transitionStatus = TransitionStatus.Ending;
        }

        context.InstantType = instantType;
        context.TransitionStatus = transitionStatus;
        context.Mounted = isMounted;
        context.ActiveIndex = activeIndex;

        if (!IsControlled)
        {
            isOpen = nextOpen;
        }

        state = new MenuRootState(nextOpen);
        context.Open = nextOpen;

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
                // Circuit-safe JS interop - intentional empty catch for disconnection during state sync
            }
        }

        await OpenChanged.InvokeAsync(nextOpen);
        StateHasChanged();
    }

    private MenuRootContext CreateContext() => new(
        rootId: rootId,
        open: CurrentOpen,
        mounted: isMounted,
        disabled: Disabled,
        parentType: ParentType,
        openChangeReason: openChangeReason,
        transitionStatus: transitionStatus,
        instantType: instantType,
        activeIndex: activeIndex,
        getOpen: () => CurrentOpen,
        getMounted: () => isMounted,
        getTriggerElement: () => triggerElement,
        setTriggerElement: SetTriggerElement,
        setPositionerElement: SetPositionerElement,
        setPopupElement: SetPopupElement,
        setActiveIndex: SetActiveIndex,
        setOpenAsync: SetOpenAsyncFromContext,
        emitClose: EmitClose);

    private async Task InitializeJsAsync()
    {
        try
        {
            var module = await ModuleTask.Value;
            var isNested = ParentType != MenuParentType.None;
            var modal = !isNested && Modal == ModalMode.True;
            await module.InvokeVoidAsync("initializeRoot", rootId, dotNetRef, CloseParentOnEsc, LoopFocus, modal);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
            // Circuit-safe JS interop - intentional empty catch for disconnection during initialization
        }
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
            // Circuit-safe JS interop - intentional empty catch for disconnection during element sync
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
            // Circuit-safe JS interop - intentional empty catch for disconnection during element sync
        }
    }

    private void SetActiveIndex(int index)
    {
        if (activeIndex == index)
        {
            return;
        }

        activeIndex = index;
        context.ActiveIndex = index;

        if (hasRendered)
        {
            _ = SetActiveIndexAsync(index);
        }
    }

    private async Task SetActiveIndexAsync(int index)
    {
        try
        {
            var module = await ModuleTask.Value;
            await module.InvokeVoidAsync("setActiveIndex", rootId, index);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
            // Circuit-safe JS interop - intentional empty catch for disconnection during index sync
        }
    }

    private Task SetOpenAsyncFromContext(bool nextOpen, OpenChangeReason reason, object? payload)
    {
        return SetOpenAsync(nextOpen, reason);
    }

    private void EmitClose(OpenChangeReason reason, object? payload)
    {
        _ = SetOpenAsync(false, reason);
    }

    private void ForceUnmount()
    {
        isMounted = false;
        context.Mounted = false;
        transitionStatus = TransitionStatus.None;
        context.TransitionStatus = transitionStatus;
        _ = InvokeOpenChangeCompleteAsync(false);
        StateHasChanged();
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
}

public sealed class MenuRootActions
{
    public Action? Unmount { get; internal set; }

    public Action? Close { get; internal set; }
}
