using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Tabs;

public sealed class TabsList<TValue> : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-tabs.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private DotNetObjectReference<TabsList<TValue>>? dotNetRef;
    private TabsListContext<TValue>? listContext;
    private TabsRootState state = TabsRootState.Default;
    private bool isComponentRenderAs;
    private bool hasRendered;
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

        if (listContext is not null)
        {
            listContext.ActivateOnFocus = ActivateOnFocus;
            listContext.LoopFocus = LoopFocus;
        }
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

        builder.AddAttribute(3, "aria-orientation", orientation == Orientation.Vertical ? "vertical" : "horizontal");

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

        dotNetRef?.Dispose();
    }

    [JSInvokable]
    public async Task OnNavigateToTab(string serializedValue)
    {
        if (RootContext is null)
        {
            return;
        }

        TValue? value;
        if (typeof(TValue) == typeof(string))
        {
            value = (TValue)(object)serializedValue;
        }
        else
        {
            try
            {
                value = System.Text.Json.JsonSerializer.Deserialize<TValue>(serializedValue);
            }
            catch (System.Text.Json.JsonException)
            {
                return;
            }
        }

        if (value is not null)
        {
            await RootContext.OnValueChangeAsync(value, ActivationDirection.None);
        }
    }

    private TabsListContext<TValue> CreateContext() => new(
        activateOnFocus: ActivateOnFocus,
        loopFocus: LoopFocus,
        getTabsListElement: () => Element,
        rootContext: RootContext!);

    private async Task InitializeJsAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            if (Element.HasValue)
            {
                dotNetRef = DotNetObjectReference.Create(this);
                var orientationString = Orientation.ToDataAttributeString() ?? "horizontal";
                await module.InvokeVoidAsync("initializeList", Element.Value, orientationString, LoopFocus, ActivateOnFocus, dotNetRef);
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (!hasRendered || !Element.HasValue)
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
            var activeElement = await GetActiveTabElementAsync();
            if (activeElement is null)
            {
                await EventUtilities.InvokeOnKeyDownAsync(AdditionalAttributes, e);
                return;
            }

            try
            {
                var module = await moduleTask.Value;
                if (shouldNavigatePrevious)
                {
                    await module.InvokeVoidAsync("navigateToPrevious", Element.Value, activeElement);
                }
                else if (shouldNavigateNext)
                {
                    await module.InvokeVoidAsync("navigateToNext", Element.Value, activeElement);
                }
                else if (e.Key == "Home")
                {
                    await module.InvokeVoidAsync("navigateToFirst", Element.Value);
                }
                else if (e.Key == "End")
                {
                    await module.InvokeVoidAsync("navigateToLast", Element.Value);
                }
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
            finally
            {
                await activeElement.DisposeAsync();
            }
        }

        await EventUtilities.InvokeOnKeyDownAsync(AdditionalAttributes, e);
    }

    private async Task<IJSObjectReference?> GetActiveTabElementAsync()
    {
        try
        {
            var module = await moduleTask.Value;
            return await module.InvokeAsync<IJSObjectReference?>("getActiveElement");
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
            return null;
        }
    }
}
