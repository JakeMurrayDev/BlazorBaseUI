using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Accordion;

public sealed class AccordionPanel : ComponentBase, IAsyncDisposable
{
    private const string DefaultTag = "div";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-collapsible.js";
    private const string CssVarPrefix = "accordion-panel";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private string? defaultId;
    private bool hasRendered;
    private bool isMounted;
    private bool jsInitialized;
    private DotNetObjectReference<AccordionPanel>? dotNetRef;
    private bool isComponentRenderAs;
    private AccordionPanelState state = new(false, false, 0, Orientation.Vertical, TransitionStatus.Undefined);
    private bool? animationTarget;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private IAccordionRootContext? RootContext { get; set; }

    [CascadingParameter]
    private IAccordionItemContext? ItemContext { get; set; }

    [Parameter]
    public bool? KeepMounted { get; set; }

    [Parameter]
    public bool? HiddenUntilFound { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<AccordionPanelState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<AccordionPanelState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool CurrentOpen => ItemContext?.Open ?? false;

    private bool ResolvedKeepMounted => KeepMounted ?? RootContext?.KeepMounted ?? false;

    private bool ResolvedHiddenUntilFound => HiddenUntilFound ?? RootContext?.HiddenUntilFound ?? false;

    private string ResolvedId
    {
        get
        {
            var id = AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());
            if (id != ItemContext?.PanelId)
            {
                ItemContext?.SetPanelId(id);
            }
            return id;
        }
    }

    private bool IsPresent => ResolvedKeepMounted || ResolvedHiddenUntilFound || isMounted;

    private bool IsHidden => !ResolvedKeepMounted && !ResolvedHiddenUntilFound && !CurrentOpen && animationTarget != false;

    public AccordionPanel()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        var initialOpen = CurrentOpen;
        isMounted = initialOpen;
        state = new AccordionPanelState(
            initialOpen,
            ItemContext?.Disabled ?? false,
            ItemContext?.Index ?? 0,
            RootContext?.Orientation ?? Orientation.Vertical,
            TransitionStatus.Undefined);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var currentOpen = CurrentOpen;
        var currentDisabled = ItemContext?.Disabled ?? false;
        var currentIndex = ItemContext?.Index ?? 0;
        var currentOrientation = RootContext?.Orientation ?? Orientation.Vertical;

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

        if (state.Open != currentOpen || state.Disabled != currentDisabled || state.Index != currentIndex || state.Orientation != currentOrientation)
        {
            state = state with { Open = currentOpen, Disabled = currentDisabled, Index = currentIndex, Orientation = currentOrientation };
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
                    await module.InvokeVoidAsync("initialize", Element.Value, dotNetRef, CurrentOpen, CssVarPrefix);
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
            if (completed && !ResolvedKeepMounted && !ResolvedHiddenUntilFound)
            {
                isMounted = false;
                jsInitialized = false;
            }
        }

        state = state with { TransitionStatus = TransitionStatus.Idle };
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!IsPresent)
        {
            return;
        }

        var userStyle = StyleValue?.Invoke(state);
        if (!jsInitialized && (ResolvedKeepMounted || ResolvedHiddenUntilFound) && !CurrentOpen)
        {
            var cssVars = $"--{CssVarPrefix}-height: 0px; --{CssVarPrefix}-width: 0px";
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
        builder.AddAttribute(3, "role", "region");

        builder.AddAttribute(4, "aria-labelledby", ItemContext?.TriggerId ?? string.Empty);

        if (IsHidden)
        {
            if (ResolvedHiddenUntilFound)
            {
                builder.AddAttribute(5, "hidden", "until-found");
            }
            else
            {
                builder.AddAttribute(6, "hidden", true);
            }
        }

        builder.AddAttribute(7, "data-index", state.Index.ToString());
        builder.AddAttribute(8, "data-orientation", state.Orientation.ToDataAttributeString());

        if (state.Open)
        {
            builder.AddAttribute(9, "data-open", string.Empty);
        }

        if (state.Disabled)
        {
            builder.AddAttribute(10, "data-disabled", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(11, "class", resolvedClass);
        }
        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(12, "style", resolvedStyle);
        }

        if (isComponentRenderAs)
        {
            builder.AddAttribute(13, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(14, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(15, elementReference => Element = elementReference);
            builder.AddContent(16, ChildContent);
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
                await module.InvokeVoidAsync("initialize", Element.Value, dotNetRef, false, CssVarPrefix);
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
