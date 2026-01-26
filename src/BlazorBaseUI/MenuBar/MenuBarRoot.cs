using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace BlazorBaseUI.MenuBar;

public sealed class MenuBarRoot : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "div";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;
    private readonly List<ElementReference> pendingRegistrations = [];
    private readonly List<ElementReference> pendingUnregistrations = [];

    private bool hasRendered;
    private Func<Task> cachedSyncJsCallback = default!;
    private bool previousDisabled;
    private Orientation previousOrientation;
    private bool previousLoopFocus;
    private bool previousModal;
    private bool isComponentRenderAs;
    private bool hasSubmenuOpen;
    private int openSubmenuCount;
    private MenuBarRootState state = default!;
    private MenuBarRootContext context = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ILogger<MenuBarRoot> Logger { get; set; } = default!;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool LoopFocus { get; set; } = true;

    [Parameter]
    public bool Modal { get; set; } = true;

    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<MenuBarRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<MenuBarRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    public MenuBarRoot()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/BlazorBaseUI/blazor-baseui-menubar.js").AsTask());
    }

    protected override void OnInitialized()
    {
        cachedSyncJsCallback = async () =>
        {
            try
            {
                await UpdateMenuBarAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error syncing JS state in {Component}", nameof(MenuBarRoot));
            }
        };

        context = new MenuBarRootContext(
            Disabled,
            hasSubmenuOpen,
            Modal,
            Orientation,
            RegisterItem,
            UnregisterItem,
            SetHasSubmenuOpen,
            GetHasSubmenuOpen,
            GetElement);

        state = new MenuBarRootState(Disabled, hasSubmenuOpen, Modal, Orientation);
    }

    private ElementReference? GetElement() => Element;

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (state.Disabled != Disabled || state.Orientation != Orientation || state.Modal != Modal || state.HasSubmenuOpen != hasSubmenuOpen)
        {
            state = new MenuBarRootState(Disabled, hasSubmenuOpen, Modal, Orientation);
            context = new MenuBarRootContext(
                Disabled,
                hasSubmenuOpen,
                Modal,
                Orientation,
                RegisterItem,
                UnregisterItem,
                SetHasSubmenuOpen,
                GetHasSubmenuOpen,
                GetElement);
        }

        if (!hasRendered)
        {
            return;
        }

        var stateChanged = Disabled != previousDisabled ||
                           Orientation != previousOrientation ||
                           LoopFocus != previousLoopFocus;
        var modalChanged = Modal != previousModal;

        previousDisabled = Disabled;
        previousOrientation = Orientation;
        previousLoopFocus = LoopFocus;
        previousModal = Modal;

        if (stateChanged)
        {
            _ = InvokeAsync(cachedSyncJsCallback);
        }

        if (modalChanged)
        {
            _ = InvokeAsync(UpdateScrollLockAsync);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientationString = Orientation.ToDataAttributeString();

        builder.OpenComponent<CascadingValue<MenuBarRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(childBuilder =>
        {
            if (isComponentRenderAs)
            {
                childBuilder.OpenRegion(0);
                childBuilder.OpenComponent(0, RenderAs!);
                childBuilder.AddMultipleAttributes(1, AdditionalAttributes);
                childBuilder.AddAttribute(2, "role", "menubar");
                childBuilder.AddAttribute(3, "aria-orientation", orientationString);
                childBuilder.AddAttribute(4, "data-orientation", orientationString);

                if (hasSubmenuOpen)
                {
                    childBuilder.AddAttribute(5, "data-has-submenu-open", "");
                }

                if (Disabled)
                {
                    childBuilder.AddAttribute(6, "data-disabled", "");
                }

                if (!string.IsNullOrEmpty(resolvedClass))
                {
                    childBuilder.AddAttribute(7, "class", resolvedClass);
                }

                if (!string.IsNullOrEmpty(resolvedStyle))
                {
                    childBuilder.AddAttribute(8, "style", resolvedStyle);
                }

                childBuilder.AddComponentParameter(9, "ChildContent", ChildContent);
                childBuilder.AddComponentReferenceCapture(10, component => { Element = ((IReferencableComponent)component).Element; });
                childBuilder.CloseComponent();
                childBuilder.CloseRegion();
            }
            else
            {
                childBuilder.OpenRegion(1);
                childBuilder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
                childBuilder.AddMultipleAttributes(1, AdditionalAttributes);
                childBuilder.AddAttribute(2, "role", "menubar");
                childBuilder.AddAttribute(3, "aria-orientation", orientationString);
                childBuilder.AddAttribute(4, "data-orientation", orientationString);

                if (hasSubmenuOpen)
                {
                    childBuilder.AddAttribute(5, "data-has-submenu-open", "");
                }

                if (Disabled)
                {
                    childBuilder.AddAttribute(6, "data-disabled", "");
                }

                if (!string.IsNullOrEmpty(resolvedClass))
                {
                    childBuilder.AddAttribute(7, "class", resolvedClass);
                }

                if (!string.IsNullOrEmpty(resolvedStyle))
                {
                    childBuilder.AddAttribute(8, "style", resolvedStyle);
                }

                childBuilder.AddElementReferenceCapture(9, elementReference => Element = elementReference);
                childBuilder.AddContent(10, ChildContent);
                childBuilder.CloseElement();
                childBuilder.CloseRegion();
            }
        }));
        builder.CloseComponent();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            previousDisabled = Disabled;
            previousOrientation = Orientation;
            previousLoopFocus = LoopFocus;

            await InitMenuBarAsync();
            await ProcessPendingRegistrationsAsync();
        }
        else
        {
            await ProcessPendingRegistrationsAsync();
        }
    }

    private void RegisterItem(ElementReference element)
    {
        if (!hasRendered)
        {
            pendingRegistrations.Add(element);
            return;
        }

        _ = InvokeAsync(async () =>
        {
            try
            {
                await RegisterItemAsync(element);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error registering item in {Component}", nameof(MenuBarRoot));
            }
        });
    }

    private void UnregisterItem(ElementReference element)
    {
        if (!hasRendered)
        {
            pendingUnregistrations.Add(element);
            return;
        }

        _ = InvokeAsync(async () =>
        {
            try
            {
                await UnregisterItemAsync(element);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error unregistering item in {Component}", nameof(MenuBarRoot));
            }
        });
    }

    private void SetHasSubmenuOpen(bool value)
    {
        // Use counter-based tracking to handle multiple menus correctly.
        // This prevents scroll lock jank when hovering between menu items:
        // - Menu A opens: count 0 → 1, hasSubmenuOpen becomes true, lock
        // - Menu B opens: count 1 → 2, hasSubmenuOpen stays true, no change
        // - Menu A closes: count 2 → 1, hasSubmenuOpen stays true, no change (no unlock!)
        // - Menu B closes: count 1 → 0, hasSubmenuOpen becomes false, unlock
        if (value)
        {
            openSubmenuCount++;
        }
        else
        {
            openSubmenuCount = Math.Max(0, openSubmenuCount - 1);
        }

        var newHasSubmenuOpen = openSubmenuCount > 0;
        if (hasSubmenuOpen == newHasSubmenuOpen)
        {
            return;
        }

        hasSubmenuOpen = newHasSubmenuOpen;
        state = new MenuBarRootState(Disabled, hasSubmenuOpen, Modal, Orientation);
        context = new MenuBarRootContext(
            Disabled,
            hasSubmenuOpen,
            Modal,
            Orientation,
            RegisterItem,
            UnregisterItem,
            SetHasSubmenuOpen,
            GetHasSubmenuOpen,
            GetElement);

        if (hasRendered)
        {
            _ = UpdateScrollLockAsync();
        }

        StateHasChanged();
    }

    private bool GetHasSubmenuOpen() => hasSubmenuOpen;

    private async Task ProcessPendingRegistrationsAsync()
    {
        foreach (var element in pendingUnregistrations)
        {
            await UnregisterItemAsync(element);
        }
        pendingUnregistrations.Clear();

        foreach (var element in pendingRegistrations)
        {
            await RegisterItemAsync(element);
        }
        pendingRegistrations.Clear();
    }

    private async Task InitMenuBarAsync()
    {
        if (!Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("initMenuBar", Element.Value, Orientation.ToDataAttributeString(), LoopFocus);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UpdateMenuBarAsync()
    {
        if (!Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("updateMenuBar", Element.Value, Orientation.ToDataAttributeString(), LoopFocus);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UpdateScrollLockAsync()
    {
        if (!Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            var shouldLock = Modal && hasSubmenuOpen;
            await module.InvokeVoidAsync("updateScrollLock", Element.Value, shouldLock);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task RegisterItemAsync(ElementReference element)
    {
        if (!Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("registerItem", Element.Value, element);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UnregisterItemAsync(ElementReference element)
    {
        if (!Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("unregisterItem", Element.Value, element);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated && Element.HasValue)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("disposeMenuBar", Element.Value);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }
}
