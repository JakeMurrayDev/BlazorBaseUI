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

    private int highlightedTabIndex;
    private TabsListContext<TValue>? listContext;
    private TabsRootState state = TabsRootState.Default;
    private bool isComponentRenderAs;
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

    public ElementReference? Element { get; private set; }

    private Orientation Orientation => RootContext?.Orientation ?? Orientation.Horizontal;

    private ActivationDirection ActivationDirection => RootContext?.ActivationDirection ?? ActivationDirection.None;

    public TabsList()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        if (RootContext is null)
        {
            throw new InvalidOperationException("TabsList must be used within a TabsRoot component.");
        }

        listContext = CreateContext();
        cachedKeyDownCallback = EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var orientation = Orientation;
        var activationDirection = ActivationDirection;

        if (state.Orientation != orientation || state.ActivationDirection != activationDirection)
        {
            state = new TabsRootState(orientation, activationDirection);
        }

        listContext?.UpdateProperties(ActivateOnFocus, highlightedTabIndex, LoopFocus);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<ITabsListContext>>(0);
        builder.AddComponentParameter(1, "Value", listContext);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)BuildInnerContent);
        builder.CloseComponent();
    }

    private void BuildInnerContent(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientation = Orientation;
        var orientationValue = orientation.ToDataAttributeString();

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "role", "tablist");

        if (orientation == Orientation.Vertical)
        {
            builder.AddAttribute(3, "aria-orientation", "vertical");
        }

        builder.AddAttribute(4, "onkeydown", cachedKeyDownCallback);

        if (orientationValue is not null)
        {
            builder.AddAttribute(5, "data-orientation", orientationValue);
        }
        builder.AddAttribute(6, "data-activation-direction", ActivationDirection.ToDataAttributeString());

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(7, "class", resolvedClass);
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(8, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(9, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(10, component =>
            {
                Element = ((IReferencableComponent)component).Element;
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(9, elementReference => Element = elementReference);
            builder.AddContent(10, ChildContent);
            builder.CloseElement();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
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

    private TabsListContext<TValue> CreateContext() => new(
        activateOnFocus: ActivateOnFocus,
        highlightedTabIndex: highlightedTabIndex,
        setHighlightedTabIndex: SetHighlightedTabIndex,
        getTabsListElement: () => Element,
        rootContext: RootContext!);

    private async Task InitializeJsAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            if (Element.HasValue)
            {
                var orientationString = Orientation.ToDataAttributeString() ?? "horizontal";
                await module.InvokeVoidAsync("initializeList", Element.Value, orientationString);
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

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (listContext is null || RootContext is null)
        {
            return;
        }

        var orientation = Orientation;
        var isHorizontal = orientation == Orientation.Horizontal;
        var isVertical = orientation == Orientation.Vertical;

        var shouldNavigatePrevious =
            (isHorizontal && e.Key == "ArrowLeft") ||
            (isVertical && e.Key == "ArrowUp");

        var shouldNavigateNext =
            (isHorizontal && e.Key == "ArrowRight") ||
            (isVertical && e.Key == "ArrowDown");

        if (shouldNavigatePrevious || shouldNavigateNext || e.Key == "Home" || e.Key == "End")
        {
            var ordered = RootContext.GetOrderedTabs();
            if (ordered.Length == 0)
            {
                return;
            }

            var currentIndex = highlightedTabIndex;
            if (currentIndex < 0 || currentIndex >= ordered.Length)
            {
                currentIndex = 0;
            }

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
}
