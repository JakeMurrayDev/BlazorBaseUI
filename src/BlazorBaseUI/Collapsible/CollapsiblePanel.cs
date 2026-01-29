using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Collapsible;

public sealed class CollapsiblePanel : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-collapsible.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private string? defaultId;
    private bool hasRendered;
    private bool isMounted;
    private bool jsInitialized;
    private DotNetObjectReference<CollapsiblePanel>? dotNetRef;
    private bool isComponentRenderAs;
    private CollapsiblePanelState state = new(false, false, TransitionStatus.Undefined);
    private bool? animationTarget;

    private bool CurrentOpen => Context?.Open ?? false;

    private string ResolvedId
    {
        get
        {
            var id = AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());
            if (id != Context?.PanelId)
            {
                Context?.SetPanelId(id);
            }
            return id;
        }
    }

    private bool IsPresent => KeepMounted || HiddenUntilFound || isMounted;

    private bool IsHidden => !KeepMounted && !HiddenUntilFound && !CurrentOpen && animationTarget != false;

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

    public CollapsiblePanel()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        var initialOpen = Context?.Open ?? false;
        isMounted = initialOpen;
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

        if (currentOpen != state.Open)
        {
            if (currentOpen)
            {
                isMounted = true;
                animationTarget = true;
            }
            else
            {
                animationTarget = false;
            }
        }

        if (state.Open != currentOpen || state.Disabled != currentDisabled)
        {
            state = state with { Open = currentOpen, Disabled = currentDisabled };
        }

        // Register panel ID with context early so trigger can reference it
        // This must happen in OnParametersSet, not during render, for StateHasChanged to work
        _ = ResolvedId;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            dotNetRef = DotNetObjectReference.Create(this);

            if (IsPresent && Element.HasValue)
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

        if (animationTarget == true)
        {
            animationTarget = null;
            _ = OpenAsync();
        }
        else if (animationTarget == false)
        {
            animationTarget = null;
            _ = CloseAsync();
        }
    }

    [JSInvokable]
    public void OnTransitionStatusChanged(string status)
    {
        // Note: We don't call StateHasChanged() here because the JS is controlling
        // the data-starting-style and data-ending-style attributes directly on the DOM.
        // Calling StateHasChanged() during animation would cause re-renders that
        // interfere with the animation timing.
        state = status switch
        {
            "starting" => state with { TransitionStatus = TransitionStatus.Starting },
            "ending" => state with { TransitionStatus = TransitionStatus.Ending },
            "idle" => state with { TransitionStatus = TransitionStatus.Idle },
            _ => state with { TransitionStatus = TransitionStatus.Undefined }
        };
    }

    [JSInvokable]
    public void OnAnimationEnded(string animationType, bool completed)
    {
        if (animationType == "close")
        {
            if (completed && !KeepMounted && !HiddenUntilFound)
            {
                isMounted = false;
                jsInitialized = false;
            }
        }

        state = state with { TransitionStatus = TransitionStatus.Idle };
        StateHasChanged();
    }

    [JSInvokable]
    public void OnBeforeMatch()
    {
        // Called when browser's "find in page" reveals hidden="until-found" content
        Context?.HandleBeforeMatch();
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
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            RenderCommonAttributes(builder);
            RenderDataAttributes(builder);
            RenderClassAndStyle(builder, resolvedClass, resolvedStyle);
            builder.AddAttribute(12, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(13, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            RenderCommonAttributes(builder);
            RenderDataAttributes(builder);
            RenderClassAndStyle(builder, resolvedClass, resolvedStyle);
            builder.AddElementReferenceCapture(12, elementReference => Element = elementReference);
            builder.AddContent(13, ChildContent);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    private void RenderCommonAttributes(RenderTreeBuilder builder)
    {
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
    }

    private void RenderDataAttributes(RenderTreeBuilder builder)
    {
        if (state.Open)
        {
            builder.AddAttribute(5, "data-open", string.Empty);
        }
        else
        {
            builder.AddAttribute(6, "data-closed", string.Empty);
        }

        if (state.TransitionStatus == TransitionStatus.Starting)
        {
            builder.AddAttribute(7, "data-starting-style", string.Empty);
        }

        if (state.TransitionStatus == TransitionStatus.Ending)
        {
            builder.AddAttribute(8, "data-ending-style", string.Empty);
        }
    }

    private void RenderClassAndStyle(RenderTreeBuilder builder, string? resolvedClass, string? resolvedStyle)
    {
        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(9, "class", resolvedClass);
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(10, "style", resolvedStyle);
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
