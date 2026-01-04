using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Tabs;

public sealed class TabsList<TValue> : ComponentBase, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-tabs.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private int highlightedTabIndex;
    private double? previousTabEdge;
    private TabsListContext<TValue>? listContext;
    private TabsRootState? cachedState;
    private bool stateDirty = true;

    private EventCallback<KeyboardEventArgs> cachedKeyDownCallback;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private ITabsRootContext<TValue>? RootContext { get; set; }

    [Parameter]
    public bool ActivateOnFocus { get; set; }

    [Parameter]
    public bool LoopFocus { get; set; } = true;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<TabsRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<TabsRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private Orientation Orientation => RootContext?.Orientation ?? Orientation.Horizontal;

    private ActivationDirection ActivationDirection => RootContext?.ActivationDirection ?? ActivationDirection.None;

    private TabsRootState State
    {
        get
        {
            if (stateDirty || cachedState is null)
            {
                cachedState = new TabsRootState(Orientation, ActivationDirection);
                stateDirty = false;
            }
            return cachedState;
        }
    }

    public TabsList()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        if (RootContext is null)
            throw new InvalidOperationException("TabsList must be used within a TabsRoot component.");

        listContext = CreateContext();

        cachedKeyDownCallback = EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync);
    }

    protected override void OnParametersSet()
    {
        listContext?.UpdateProperties(ActivateOnFocus, highlightedTabIndex, LoopFocus);
        stateDirty = true;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<ITabsListContext>>(0);
        builder.AddComponentParameter(1, "Value", listContext);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)RenderList);
        builder.CloseComponent();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            await InitializeJsAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask.IsValueCreated && Element.HasValue)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("disposeList", Element.Value);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    private void RenderList(RenderTreeBuilder builder)
    {
        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (RenderAs is not null)
        {
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, BuildAttributes(state, resolvedClass, resolvedStyle));
            builder.AddComponentParameter(2, "ChildContent", ChildContent);
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

    private Dictionary<string, object> BuildAttributes(TabsRootState state, string? resolvedClass, string? resolvedStyle)
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

        attributes["role"] = "tablist";

        if (Orientation == Orientation.Vertical)
            attributes["aria-orientation"] = "vertical";

        attributes["onkeydown"] = cachedKeyDownCallback;

        state.WriteDataAttributes(attributes);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        return attributes;
    }

    private TabsListContext<TValue> CreateContext() => new(
        activateOnFocus: ActivateOnFocus,
        highlightedTabIndex: highlightedTabIndex,
        setHighlightedTabIndex: SetHighlightedTabIndex,
        getTabsListElement: () => Element,
        rootContext: RootContext!,
        focusTabAtIndex: FocusTabAtIndexAsync);

    private async Task InitializeJsAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            if (Element.HasValue)
            {
                await module.InvokeVoidAsync("initializeList", Element.Value);
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private void SetHighlightedTabIndex(int index)
    {
        highlightedTabIndex = index;
        listContext?.UpdateProperties(ActivateOnFocus, highlightedTabIndex, LoopFocus);
    }

    private async Task FocusTabAtIndexAsync(int index)
    {
        if (RootContext is not TabsRootContext<TValue> ctx)
            return;

        var ordered = ctx.GetOrderedTabs();
        if (index >= 0 && index < ordered.Length)
        {
            await ordered[index].Focus();
        }
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (listContext is null)
            return;

        var isHorizontal = Orientation == Orientation.Horizontal;
        var isVertical = Orientation == Orientation.Vertical;

        var shouldNavigatePrevious =
            (isHorizontal && e.Key == "ArrowLeft") ||
            (isVertical && e.Key == "ArrowUp");

        var shouldNavigateNext =
            (isHorizontal && e.Key == "ArrowRight") ||
            (isVertical && e.Key == "ArrowDown");

        if (shouldNavigatePrevious || shouldNavigateNext || e.Key == "Home" || e.Key == "End")
        {
            if (RootContext is not TabsRootContext<TValue> ctx)
                return;

            var ordered = ctx.GetOrderedTabs();
            if (ordered.Length == 0)
                return;

            var currentIndex = highlightedTabIndex;
            if (currentIndex < 0 || currentIndex >= ordered.Length)
                currentIndex = 0;

            var currentTab = ordered[currentIndex].Tab;

            if (shouldNavigatePrevious)
            {
                await listContext.NavigateToPreviousAsync(currentTab);
            }
            else if (shouldNavigateNext)
            {
                await listContext.NavigateToNextAsync(currentTab);
            }
            else if (e.Key == "Home")
            {
                await listContext.NavigateToFirstAsync();
            }
            else if (e.Key == "End")
            {
                await listContext.NavigateToLastAsync();
            }
        }
    }

    internal ActivationDirection DetectActivationDirection(TValue? newValue)
    {
        if (RootContext is null)
            return ActivationDirection.None;

        var currentValue = RootContext.Value;

        if (EqualityComparer<TValue>.Default.Equals(newValue, currentValue))
            return ActivationDirection.None;

        if (newValue is null)
        {
            previousTabEdge = null;
            return ActivationDirection.None;
        }

        if (RootContext is not TabsRootContext<TValue> ctx)
            return ActivationDirection.None;

        var newTabElement = ctx.GetTabElementByValue(newValue);
        if (newTabElement is null)
            return ActivationDirection.None;

        if (previousTabEdge is null)
            return ActivationDirection.None;

        var isHorizontal = Orientation == Orientation.Horizontal;

        return isHorizontal
            ? (previousTabEdge > 0 ? ActivationDirection.Left : ActivationDirection.Right)
            : (previousTabEdge > 0 ? ActivationDirection.Up : ActivationDirection.Down);
    }

    internal async Task<ActivationDirection> DetectActivationDirectionAsync(TValue? newValue)
    {
        if (!hasRendered || RootContext is null || !Element.HasValue)
            return ActivationDirection.None;

        var currentValue = RootContext.Value;

        if (EqualityComparer<TValue>.Default.Equals(newValue, currentValue))
            return ActivationDirection.None;

        if (newValue is null)
        {
            previousTabEdge = null;
            return ActivationDirection.None;
        }

        var newTabElement = RootContext.GetTabElementByValue(newValue);
        if (newTabElement is null || !newTabElement.HasValue)
            return ActivationDirection.None;

        try
        {
            var module = await moduleTask.Value;
            var newPosition = await module.InvokeAsync<TabPositionResult>("getTabPosition", Element.Value, newTabElement.Value);
            var currentTabElement = RootContext.GetTabElementByValue(currentValue);

            if (currentTabElement is null || !currentTabElement.HasValue)
            {
                previousTabEdge = Orientation == Orientation.Horizontal ? newPosition.Left : newPosition.Top;
                return ActivationDirection.None;
            }

            var currentPosition = await module.InvokeAsync<TabPositionResult>("getTabPosition", Element.Value, currentTabElement.Value);

            var isHorizontal = Orientation == Orientation.Horizontal;
            var currentEdge = isHorizontal ? currentPosition.Left : currentPosition.Top;
            var newEdge = isHorizontal ? newPosition.Left : newPosition.Top;

            previousTabEdge = newEdge;

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
