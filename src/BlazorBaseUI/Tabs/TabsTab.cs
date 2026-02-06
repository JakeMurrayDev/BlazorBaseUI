using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Tabs;

public sealed class TabsTab<TValue> : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "button";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-tabs.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private bool hasRendered;
    private bool previousActive;
    private bool previousDisabled;
    private Orientation previousOrientation;
    private string? defaultId;
    private string tabId = null!;
    private TabsTabState state = TabsTabState.Default;
    private bool isComponentRenderAs;

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

    public ElementReference? Element { get; private set; }

    private bool IsActive
    {
        get
        {
            if (RootContext is null)
            {
                return false;
            }

            return EqualityComparer<TValue>.Default.Equals(RootContext.Value, Value);
        }
    }

    private Orientation Orientation => RootContext?.Orientation ?? Orientation.Horizontal;

    private int TabIndex
    {
        get
        {
            if (Disabled)
            {
                return -1;
            }

            if (RootContext is null)
            {
                return 0;
            }

            if (IsActive)
            {
                return 0;
            }

            if (RootContext.Value is null)
            {
                return 0;
            }

            return -1;
        }
    }

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    public TabsTab()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        if (RootContext is null)
        {
            throw new InvalidOperationException("TabsTab must be used within a TabsRoot component.");
        }

        tabId = ResolvedId;
        previousActive = IsActive;
        previousDisabled = Disabled;
        previousOrientation = Orientation;
        state = new TabsTabState(previousActive, previousDisabled, previousOrientation);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;

        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var newId = ResolvedId;
        if (newId != tabId)
        {
            tabId = newId;
        }

        var currentActive = IsActive;
        var currentDisabled = Disabled;
        var currentOrientation = Orientation;

        if (currentActive != previousActive ||
            currentDisabled != previousDisabled ||
            currentOrientation != previousOrientation)
        {
            state = new TabsTabState(currentActive, currentDisabled, currentOrientation);
            previousActive = currentActive;
            previousDisabled = currentDisabled;
            previousOrientation = currentOrientation;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientationValue = Orientation.ToDataAttributeString();
        var isActive = IsActive;
        var panelId = RootContext?.GetTabPanelIdByValue(Value);

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "id", tabId);
        builder.AddAttribute(3, "role", "tab");
        builder.AddAttribute(4, "aria-selected", isActive ? "true" : "false");
        builder.AddAttribute(5, "tabindex", TabIndex);

        builder.AddAttribute(6, "aria-controls", panelId);

        if (NativeButton)
        {
            builder.AddAttribute(7, "type", "button");

            if (Disabled)
            {
                builder.AddAttribute(8, "disabled", true);
            }
        }
        else
        {
            if (Disabled)
            {
                builder.AddAttribute(8, "aria-disabled", "true");
            }
        }

        builder.AddAttribute(9, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));
        builder.AddAttribute(10, "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocusAsync));

        if (orientationValue is not null)
        {
            builder.AddAttribute(11, "data-orientation", orientationValue);
        }

        if (isActive)
        {
            builder.AddAttribute(12, "data-active", string.Empty);
        }

        if (Disabled)
        {
            builder.AddAttribute(13, "data-disabled", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(14, "class", resolvedClass);
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(15, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddComponentParameter(16, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(17, component =>
            {
                Element = ((IReferencableComponent)component).Element;
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(16, elementReference => Element = elementReference);
            builder.AddContent(17, ChildContent);
            builder.CloseElement();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && Element.HasValue)
        {
            hasRendered = true;
            RootContext?.RegisterTabInfo(Value, Element.Value, tabId, Disabled);

            if (!NativeButton)
            {
                await InitializeJsAsync();
            }

            await RegisterWithListAsync();
        }
        else if (hasRendered && Element.HasValue)
        {
            RootContext?.RegisterTabInfo(Value, Element.Value, tabId, Disabled);
            await RegisterWithListAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        RootContext?.UnregisterTabInfo(Value);

        if (moduleTask.IsValueCreated)
        {
            try
            {
                await UnregisterFromListAsync();
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

    private async Task RegisterWithListAsync()
    {
        if (ListContext?.TabsListElement is null || !ListContext.TabsListElement.HasValue || !Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            var serializedValue = SerializeValue(Value);
            await module.InvokeVoidAsync("registerTab", ListContext.TabsListElement.Value, Element.Value, serializedValue);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UnregisterFromListAsync()
    {
        if (ListContext?.TabsListElement is null || !ListContext.TabsListElement.HasValue || !Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("unregisterTab", ListContext.TabsListElement.Value, Element.Value);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private static string? SerializeValue(TValue? value)
    {
        if (value is null)
            return null;

        if (value is string str)
            return str;

        return System.Text.Json.JsonSerializer.Serialize(value);
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
        {
            return;
        }

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
        {
            return;
        }

        await ActivateTabAsync();
        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }

    private async Task HandleFocusAsync(FocusEventArgs e)
    {
        if (Disabled)
        {
            return;
        }

        if (ListContext?.ActivateOnFocus == true && !IsActive)
        {
            await ActivateTabAsync();
        }

        await EventUtilities.InvokeOnFocusAsync(AdditionalAttributes, e);
    }

    private async Task ActivateTabAsync()
    {
        if (ListContext is null || RootContext is null)
        {
            return;
        }

        var direction = await DetectActivationDirectionAsync();
        await ListContext.OnTabActivationAsync(Value, direction);
    }

    private async Task<ActivationDirection> DetectActivationDirectionAsync()
    {
        if (!hasRendered || RootContext is null || ListContext?.TabsListElement is null || !Element.HasValue)
        {
            return ActivationDirection.None;
        }

        var currentValue = RootContext.Value;
        var currentTabElement = RootContext.GetTabElementByValue(currentValue);

        if (currentTabElement is null || !currentTabElement.HasValue)
        {
            return ActivationDirection.None;
        }

        try
        {
            var module = await moduleTask.Value;
            var listElement = ListContext.TabsListElement.Value;

            var currentPosition = await module.InvokeAsync<TabPositionResult?>("getTabPosition", listElement, currentTabElement.Value);
            var newPosition = await module.InvokeAsync<TabPositionResult?>("getTabPosition", listElement, Element.Value);

            if (currentPosition is null || newPosition is null)
            {
                return ActivationDirection.None;
            }

            var orientation = Orientation;
            var isHorizontal = orientation == Orientation.Horizontal;
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
