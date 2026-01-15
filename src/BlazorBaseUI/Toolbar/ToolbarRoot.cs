using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Toolbar;

public sealed class ToolbarRoot : ComponentBase, IReferencableComponent, IAsyncDisposable
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
    private bool isComponentRenderAs;
    private ToolbarRootState state = default!;
    private ToolbarRootContext context = default!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    private ILogger<ToolbarRoot> Logger { get; set; } = default!;

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool LoopFocus { get; set; } = true;

    [Parameter]
    public Orientation Orientation { get; set; } = Orientation.Horizontal;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<ToolbarRootState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<ToolbarRootState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    public ToolbarRoot()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/BlazorBaseUI/blazor-baseui-toolbar.js").AsTask());
    }

    protected override void OnInitialized()
    {
        cachedSyncJsCallback = async () =>
        {
            try
            {
                await UpdateToolbarAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error syncing JS state in {Component}", nameof(ToolbarRoot));
            }
        };

        context = new ToolbarRootContext(
            Disabled,
            Orientation,
            RegisterItem,
            UnregisterItem);

        state = new ToolbarRootState(Disabled, Orientation);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (state.Disabled != Disabled || state.Orientation != Orientation)
        {
            state = new ToolbarRootState(Disabled, Orientation);
            context = new ToolbarRootContext(Disabled, Orientation, RegisterItem, UnregisterItem);
        }

        if (!hasRendered)
        {
            return;
        }

        var stateChanged = Disabled != previousDisabled ||
                           Orientation != previousOrientation ||
                           LoopFocus != previousLoopFocus;

        if (!stateChanged)
        {
            return;
        }

        previousDisabled = Disabled;
        previousOrientation = Orientation;
        previousLoopFocus = LoopFocus;

        _ = InvokeAsync(cachedSyncJsCallback);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var orientationString = Orientation.ToDataAttributeString();

        builder.OpenComponent<CascadingValue<ToolbarRootContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(childBuilder =>
        {
            if (isComponentRenderAs)
            {
                childBuilder.OpenComponent(4, RenderAs!);
            }
            else
            {
                childBuilder.OpenElement(5, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            }

            childBuilder.AddMultipleAttributes(6, AdditionalAttributes);
            childBuilder.AddAttribute(7, "role", "toolbar");
            childBuilder.AddAttribute(8, "aria-orientation", orientationString);
            childBuilder.AddAttribute(9, "data-orientation", orientationString);

            if (Disabled)
            {
                childBuilder.AddAttribute(10, "data-disabled", "");
            }

            if (!string.IsNullOrEmpty(resolvedClass))
            {
                childBuilder.AddAttribute(11, "class", resolvedClass);
            }

            if (!string.IsNullOrEmpty(resolvedStyle))
            {
                childBuilder.AddAttribute(12, "style", resolvedStyle);
            }

            if (isComponentRenderAs)
            {
                childBuilder.AddComponentParameter(13, "ChildContent", ChildContent);
                childBuilder.AddComponentReferenceCapture(14, component => { Element = ((IReferencableComponent)component).Element; });
                childBuilder.CloseComponent();
            }
            else
            {
                childBuilder.AddElementReferenceCapture(15, elementReference => Element = elementReference);
                childBuilder.AddContent(16, ChildContent);
                childBuilder.CloseElement();
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

            await InitToolbarAsync();
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
                Logger.LogError(ex, "Error registering item in {Component}", nameof(ToolbarRoot));
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
                Logger.LogError(ex, "Error unregistering item in {Component}", nameof(ToolbarRoot));
            }
        });
    }

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

    private async Task InitToolbarAsync()
    {
        if (!Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("initToolbar", Element.Value, Orientation.ToDataAttributeString(), LoopFocus);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task UpdateToolbarAsync()
    {
        if (!Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("updateToolbar", Element.Value, Orientation.ToDataAttributeString(), LoopFocus);
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
                await module.InvokeVoidAsync("disposeToolbar", Element.Value);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }
}
