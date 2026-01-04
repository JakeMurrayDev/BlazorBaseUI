using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace BlazorBaseUI.Tabs;

public sealed class TabsIndicator<TValue> : ComponentBase, IAsyncDisposable
{
    private const string DefaultTag = "span";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-tabs.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private bool isObserving;
    private DotNetObjectReference<TabsIndicator<TValue>>? dotNetRef;
    private IndicatorPositionResult? currentPosition;
    private TabsIndicatorState? cachedState;
    private bool stateDirty = true;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private ITabsRootContext<TValue>? RootContext { get; set; }

    [CascadingParameter]
    private ITabsListContext? ListContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<TabsIndicatorState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<TabsIndicatorState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private Orientation Orientation => RootContext?.Orientation ?? Orientation.Horizontal;

    private ActivationDirection ActivationDirection => RootContext?.ActivationDirection ?? ActivationDirection.None;

    private bool HasValidPosition => currentPosition is not null && currentPosition.Width > 0 && currentPosition.Height > 0;

    private TabPosition? ActiveTabPosition => currentPosition is not null
        ? new TabPosition(currentPosition.Left, currentPosition.Right, currentPosition.Top, currentPosition.Bottom)
        : null;

    private TabSize? ActiveTabSize => currentPosition is not null
        ? new TabSize(currentPosition.Width, currentPosition.Height)
        : null;

    private TabsIndicatorState State
    {
        get
        {
            if (stateDirty || cachedState is null)
            {
                cachedState = new TabsIndicatorState(Orientation, ActivationDirection, ActiveTabPosition, ActiveTabSize);
                stateDirty = false;
            }
            return cachedState;
        }
    }

    public TabsIndicator()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnParametersSet()
    {
        stateDirty = true;

        if (hasRendered)
        {
            _ = UpdateIndicatorPositionAsync();
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is { Value: null })
            return;

        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var positionStyle = BuildPositionStyle();
        var combinedStyle = CombineStyles(resolvedStyle, positionStyle);

        if (RenderAs is not null)
        {
            if (!typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
            {
                throw new InvalidOperationException($"Type {RenderAs.Name} must implement IReferencableComponent.");
            }
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, BuildAttributes(state, resolvedClass, combinedStyle));
            builder.AddComponentParameter(2, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(3, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(3, tag);
        builder.AddMultipleAttributes(4, BuildAttributes(state, resolvedClass, combinedStyle));
        builder.AddElementReferenceCapture(5, e => Element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            dotNetRef = DotNetObjectReference.Create(this);
            await UpdateIndicatorPositionAsync();
            await StartObservingAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopObservingAsync();
        dotNetRef?.Dispose();

        if (moduleTask.IsValueCreated)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    [JSInvokable]
    public async Task OnResizeAsync()
    {
        await UpdateIndicatorPositionAsync();
        await InvokeAsync(StateHasChanged);
    }

    private Dictionary<string, object> BuildAttributes(TabsIndicatorState state, string? resolvedClass, string? resolvedStyle)
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

        attributes["role"] = "presentation";

        if (!HasValidPosition)
            attributes["hidden"] = true;

        state.WriteDataAttributes(attributes);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        return attributes;
    }

    private string? BuildPositionStyle()
    {
        if (!HasValidPosition || currentPosition is null)
            return null;

        return string.Create(CultureInfo.InvariantCulture,
            $"--tabs-indicator-active-tab-left:{currentPosition.Left}px;" +
            $"--tabs-indicator-active-tab-right:{currentPosition.Right}px;" +
            $"--tabs-indicator-active-tab-top:{currentPosition.Top}px;" +
            $"--tabs-indicator-active-tab-bottom:{currentPosition.Bottom}px;" +
            $"--tabs-indicator-active-tab-width:{currentPosition.Width}px;" +
            $"--tabs-indicator-active-tab-height:{currentPosition.Height}px;");
    }

    private static string? CombineStyles(string? style1, string? style2)
    {
        if (string.IsNullOrEmpty(style1) && string.IsNullOrEmpty(style2))
            return null;

        if (string.IsNullOrEmpty(style1))
            return style2;

        if (string.IsNullOrEmpty(style2))
            return style1;

        var s1 = style1.TrimEnd();
        if (!s1.EndsWith(';'))
            s1 += ";";

        return s1 + style2;
    }

    private async Task UpdateIndicatorPositionAsync()
    {
        if (!hasRendered || RootContext is null || ListContext?.TabsListElement is null)
        {
            currentPosition = null;
            stateDirty = true;
            return;
        }

        var activeValue = RootContext.Value;
        if (activeValue is null)
        {
            currentPosition = null;
            stateDirty = true;
            return;
        }

        var activeTabElement = RootContext.GetTabElementByValue(activeValue);
        if (activeTabElement is null || !activeTabElement.HasValue)
        {
            currentPosition = null;
            stateDirty = true;
            return;
        }

        var listElement = ListContext.TabsListElement;
        if (!listElement.HasValue)
        {
            currentPosition = null;
            stateDirty = true;
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            currentPosition = await module.InvokeAsync<IndicatorPositionResult>(
                "getIndicatorPosition",
                listElement.Value,
                activeTabElement.Value);
            stateDirty = true;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
            currentPosition = null;
            stateDirty = true;
        }
    }

    private async Task StartObservingAsync()
    {
        if (isObserving || ListContext?.TabsListElement is null || !ListContext.TabsListElement.HasValue)
            return;

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("observeResize", ListContext.TabsListElement.Value, dotNetRef);
            isObserving = true;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task StopObservingAsync()
    {
        if (!isObserving || !moduleTask.IsValueCreated || ListContext?.TabsListElement is null || !ListContext.TabsListElement.HasValue)
            return;

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("unobserveResize", ListContext.TabsListElement.Value);
            isObserving = false;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private sealed record IndicatorPositionResult(
        double Left,
        double Right,
        double Top,
        double Bottom,
        double Width,
        double Height);
}
