using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Tabs;

public sealed class TabsTab<TValue> : ComponentBase, IAsyncDisposable
{
    private const string DefaultTag = "button";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-tabs.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private bool previousActive;
    private bool previousDisabled;
    private string? defaultId;
    private string tabId = null!;
    private TabsTabState? cachedState;
    private bool stateDirty = true;

    private EventCallback<MouseEventArgs> cachedClickCallback;
    private EventCallback<FocusEventArgs> cachedFocusCallback;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private ITabsRootContext<TValue>? RootContext { get; set; }

    [CascadingParameter]
    private ITabsListContext? ListContext { get; set; }

    [Parameter, EditorRequired]
    public TValue Value { get; set; } = default!;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool NativeButton { get; set; } = true;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<TabsTabState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<TabsTabState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private bool IsActive
    {
        get
        {
            if (RootContext is null)
                return false;

            return EqualityComparer<TValue>.Default.Equals(RootContext.Value, Value);
        }
    }

    private Orientation Orientation => RootContext?.Orientation ?? Orientation.Horizontal;

    private int TabIndex
    {
        get
        {
            if (Disabled)
                return -1;

            if (ListContext is null || RootContext is null)
                return 0;

            var highlightedIndex = ListContext.HighlightedTabIndex;

            var myIndex = RootContext.GetTabIndex(this);
            if (myIndex == highlightedIndex)
                return 0;

            if (RootContext.Value is null)
            {
                var firstEnabled = RootContext.GetFirstEnabledTab();
                if (firstEnabled is not null && ReferenceEquals(firstEnabled.Tab, this))
                    return 0;
            }

            return -1;
        }
    }

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    private TabsTabState State
    {
        get
        {
            if (stateDirty || cachedState is null)
            {
                cachedState = new TabsTabState(IsActive, Disabled, Orientation);
                stateDirty = false;
            }
            return cachedState;
        }
    }

    public TabsTab()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        if (RootContext is null)
            throw new InvalidOperationException("TabsTab must be used within a TabsRoot component.");

        tabId = ResolvedId;

        previousActive = IsActive;
        previousDisabled = Disabled;

        cachedClickCallback = EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync);
        cachedFocusCallback = EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocusAsync);
    }

    protected override void OnParametersSet()
    {
        var newId = ResolvedId;
        if (newId != tabId)
        {
            tabId = newId;
        }

        var currentActive = IsActive;
        var currentDisabled = Disabled;

        if (currentActive != previousActive || currentDisabled != previousDisabled)
        {
            stateDirty = true;
            previousActive = currentActive;
            previousDisabled = currentDisabled;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (RenderAs is not null)
        {
            if (!typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
            {
                throw new InvalidOperationException($"Type {RenderAs.Name} must implement IReferencableElement.");
            }
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, BuildAttributes(state, resolvedClass, resolvedStyle));
            builder.AddComponentParameter(2, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(3, component => { Element = ((IReferencableComponent)component).Element!; });
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(3, tag);
        builder.AddMultipleAttributes(4, BuildAttributes(state, resolvedClass, resolvedStyle));
        builder.AddElementReferenceCapture(5, e => Element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Element.HasValue)
        {
            hasRendered = true;
            RootContext?.RegisterTab(this, Element.Value, Value, tabId, () => Disabled, FocusAsync);

            if (!NativeButton)
            {
                await InitializeJsAsync();
            }
        }
        else if (hasRendered && Element.HasValue)
        {
            RootContext?.RegisterTab(this, Element.Value, Value, tabId, () => Disabled, FocusAsync);
        }
    }

    public async ValueTask DisposeAsync()
    {
        RootContext?.UnregisterTab(this);

        if (moduleTask.IsValueCreated)
        {
            try
            {
                var module = await moduleTask.Value;

                if (!NativeButton && Element.HasValue)
                {
                    await module.InvokeVoidAsync("dispose", Element.Value);
                }

                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    private Dictionary<string, object> BuildAttributes(TabsTabState state, string? resolvedClass, string? resolvedStyle)
    {
        var attributes = new Dictionary<string, object>();

        if (AdditionalAttributes is not null)
        {
            foreach (var attr in AdditionalAttributes)
            {
                if (attr.Key is not "class" and not "style")
                    attributes[attr.Key] = attr.Value;
            }
        }

        attributes["id"] = tabId;
        attributes["role"] = "tab";
        attributes["aria-selected"] = IsActive ? "true" : "false";
        attributes["tabindex"] = TabIndex;

        var panelId = RootContext?.GetTabPanelIdByValue(Value);
        if (panelId is not null)
            attributes["aria-controls"] = panelId;

        if (NativeButton)
        {
            attributes["type"] = "button";

            if (Disabled)
                attributes["disabled"] = true;
        }
        else
        {
            if (Disabled)
                attributes["aria-disabled"] = "true";
        }

        attributes["onclick"] = cachedClickCallback;
        attributes["onfocus"] = cachedFocusCallback;

        state.WriteDataAttributes(attributes);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        return attributes;
    }

    private async Task InitializeJsAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            if (Element.HasValue)
            {
                await module.InvokeVoidAsync("initializeTab", Element.Value);
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    internal async ValueTask FocusAsync()
    {
        if (!hasRendered)
            return;

        try
        {
            var module = await moduleTask.Value;
            if (Element.HasValue)
            {
                await module.InvokeVoidAsync("focus", Element.Value);
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (Disabled || IsActive)
            return;

        await ActivateTabAsync();
    }

    private async Task HandleFocusAsync(FocusEventArgs e)
    {
        if (Disabled)
            return;

        if (RootContext is not null && ListContext is not null)
        {
            var myIndex = RootContext.GetTabIndex(this);
            if (myIndex >= 0)
            {
                ListContext.SetHighlightedTabIndex(myIndex);
            }
        }

        if (ListContext?.ActivateOnFocus == true && !IsActive)
        {
            await ActivateTabAsync();
        }
    }

    private async Task ActivateTabAsync()
    {
        if (ListContext is null || RootContext is null)
            return;

        var direction = await DetectActivationDirectionAsync();
        await ListContext.OnTabActivationAsync(Value, direction);
    }

    private async Task<ActivationDirection> DetectActivationDirectionAsync()
    {
        if (!hasRendered || RootContext is null || ListContext?.TabsListElement is null || !Element.HasValue)
            return ActivationDirection.None;

        var currentValue = RootContext.Value;
        var currentTabElement = RootContext.GetTabElementByValue(currentValue);

        if (currentTabElement is null || !currentTabElement.HasValue)
            return ActivationDirection.None;

        try
        {
            var module = await moduleTask.Value;
            var listElement = ListContext.TabsListElement.Value;

            var currentPosition = await module.InvokeAsync<TabPositionResult>("getTabPosition", listElement, currentTabElement.Value);
            var newPosition = await module.InvokeAsync<TabPositionResult>("getTabPosition", listElement, Element.Value);

            var isHorizontal = Orientation == Orientation.Horizontal;
            var currentEdge = isHorizontal ? currentPosition.Left : currentPosition.Top;
            var newEdge = isHorizontal ? newPosition.Left : newPosition.Top;

            if (isHorizontal)
            {
                return newEdge > currentEdge ? ActivationDirection.Right : ActivationDirection.Left;
            }
            else
            {
                return newEdge > currentEdge ? ActivationDirection.Down : ActivationDirection.Up;
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
            return ActivationDirection.None;
        }
    }

    private sealed record TabPositionResult(double Left, double Top, double Width, double Height);
}
