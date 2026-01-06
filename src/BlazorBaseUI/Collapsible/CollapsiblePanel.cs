using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Collapsible;

public sealed class CollapsiblePanel : ComponentBase, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-collapsible.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private string? defaultId;
    private bool hasRendered;
    private bool isMounted;
    private bool previousOpen;
    private bool pendingOpen;
    private bool pendingClose;
    private bool jsInitialized;
    private DotNetObjectReference<CollapsiblePanel>? dotNetRef;
    private bool isComponentRenderAs;
    private CollapsiblePanelState state = new(false, false, TransitionStatus.Undefined);

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private CollapsibleRootContext? Context { get; set; }

    [Parameter]
    public bool KeepMounted { get; set; }

    [Parameter]
    public bool HiddenUntilFound { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<CollapsiblePanelState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<CollapsiblePanelState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool CurrentOpen => Context?.Open ?? false;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    private bool IsPresent => KeepMounted || HiddenUntilFound || isMounted;

    private bool IsHidden => !KeepMounted && !HiddenUntilFound && !CurrentOpen && !pendingClose;

    public CollapsiblePanel()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        var initialOpen = Context?.Open ?? false;
        isMounted = initialOpen;
        previousOpen = initialOpen;
        state = new CollapsiblePanelState(initialOpen, Context?.Disabled ?? false, TransitionStatus.Undefined);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var currentOpen = CurrentOpen;
        var currentDisabled = Context?.Disabled ?? false;

        if (currentOpen && !previousOpen)
        {
            isMounted = true;
            pendingOpen = true;
            pendingClose = false;
        }
        else if (!currentOpen && previousOpen)
        {
            pendingClose = true;
            pendingOpen = false;
        }

        previousOpen = currentOpen;

        if (state.Open != currentOpen || state.Disabled != currentDisabled)
        {
            state = new CollapsiblePanelState(currentOpen, currentDisabled, TransitionStatus.Undefined);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            dotNetRef = DotNetObjectReference.Create(this);

            if (isMounted && Element.HasValue)
            {
                try
                {
                    var module = await moduleTask.Value;
                    await module.InvokeVoidAsync("initialize", Element.Value, dotNetRef, CurrentOpen);
                    jsInitialized = true;
                }
                catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
                {
                }
            }

            return;
        }

        if (pendingOpen)
        {
            pendingOpen = false;
            await OpenAsync();
        }
        else if (pendingClose)
        {
            await CloseAsync();
        }
    }

    [JSInvokable]
    public async Task OnOpenAnimationComplete()
    {
        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public async Task OnCloseAnimationComplete()
    {
        pendingClose = false;
        isMounted = false;

        if (!KeepMounted && !HiddenUntilFound)
        {
            jsInitialized = false;
        }

        await InvokeAsync(StateHasChanged);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!IsPresent)
        {
            return;
        }

        var userStyle = StyleValue?.Invoke(state);
        if (!jsInitialized && (KeepMounted || HiddenUntilFound) && !CurrentOpen)
        {
            var cssVars = "--collapsible-panel-height: 0px; --collapsible-panel-width: 0px";
            userStyle = string.IsNullOrEmpty(userStyle) ? cssVars : $"{cssVars}; {userStyle}";
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, userStyle);

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);

        builder.AddAttribute(2, "id", ResolvedId);

        if (IsHidden)
        {
            if (HiddenUntilFound)
            {
                builder.AddAttribute(3, "hidden", "until-found");
            }
            else
            {
                builder.AddAttribute(4, "hidden", true);
            }
        }

        if (state.Open)
        {
            builder.AddAttribute(5, "data-open", string.Empty);
        }
        else
        {
            builder.AddAttribute(6, "data-closed", string.Empty);
        }

        if (state.Disabled)
        {
            builder.AddAttribute(7, "data-disabled", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(8, "class", resolvedClass);
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(9, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddAttribute(10, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(11, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(12, elementReference => Element = elementReference);
            builder.AddContent(13, ChildContent);
            builder.CloseElement();
        }
    }

    private async Task OpenAsync()
    {
        if (!hasRendered || !Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            dotNetRef ??= DotNetObjectReference.Create(this);

            if (!jsInitialized)
            {
                await module.InvokeVoidAsync("initialize", Element.Value, dotNetRef, false);
                jsInitialized = true;
            }

            await module.InvokeVoidAsync("open", Element.Value, false);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task CloseAsync()
    {
        if (!hasRendered || !Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await moduleTask.Value;
            await module.InvokeVoidAsync("close", Element.Value);
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
                await module.InvokeVoidAsync("dispose", Element.Value);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }

        dotNetRef?.Dispose();
    }
}
