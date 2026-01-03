using System.Diagnostics.CodeAnalysis;
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
    private bool previousOpen;
    private bool pendingOpen;
    private bool pendingClose;
    private bool jsInitialized;
    private DotNetObjectReference<AccordionPanel>? dotNetRef;

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

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private bool CurrentOpen => ItemContext?.Open ?? false;

    private bool ResolvedKeepMounted => KeepMounted ?? RootContext?.KeepMounted ?? false;

    private bool ResolvedHiddenUntilFound => HiddenUntilFound ?? RootContext?.HiddenUntilFound ?? false;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= ItemContext?.PanelId ?? Guid.NewGuid().ToIdString());

    private bool IsPresent => ResolvedKeepMounted || ResolvedHiddenUntilFound || isMounted;

    private bool IsHidden => !ResolvedKeepMounted && !ResolvedHiddenUntilFound && !CurrentOpen && !pendingClose;

    private AccordionPanelState State => new(
        ItemContext?.Open ?? false,
        ItemContext?.Disabled ?? false,
        ItemContext?.Index ?? 0,
        RootContext?.Orientation ?? Orientation.Vertical,
        TransitionStatus.Undefined);

    public AccordionPanel()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        var initialOpen = CurrentOpen;
        isMounted = initialOpen;
        previousOpen = initialOpen;
    }

    protected override void OnParametersSet()
    {
        var currentOpen = CurrentOpen;

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
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            dotNetRef = DotNetObjectReference.Create(this);

            if (isMounted)
            {
                try
                {
                    var module = await moduleTask.Value;
                    if (Element.HasValue)
                    {
                        await module.InvokeVoidAsync("initialize", Element.Value, dotNetRef, CurrentOpen, CssVarPrefix);
                    }

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

        if (!ResolvedKeepMounted && !ResolvedHiddenUntilFound)
        {
            jsInitialized = false;
        }

        await InvokeAsync(StateHasChanged);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!IsPresent)
            return;
        
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(State));
        var resolvedStyle = BuildStyle(State);
        var attributes = BuildAttributes(State);

        if (!string.IsNullOrEmpty(resolvedClass))
            attributes["class"] = resolvedClass;
        if (!string.IsNullOrEmpty(resolvedStyle))
            attributes["style"] = resolvedStyle;

        if (RenderAs is not null)
        {
            builder.OpenComponent(0, RenderAs);
            builder.AddMultipleAttributes(1, attributes);
            builder.AddComponentParameter(2, "ChildContent", ChildContent);
            builder.CloseComponent();
            return;
        }

        var tag = !string.IsNullOrEmpty(As) ? As : DefaultTag;
        builder.OpenElement(3, tag);
        builder.AddMultipleAttributes(4, attributes);
        builder.AddElementReferenceCapture(5, e => Element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    private string? BuildStyle(AccordionPanelState state)
    {
        var userStyle = StyleValue?.Invoke(state);

        if (!jsInitialized && (ResolvedKeepMounted || ResolvedHiddenUntilFound) && !CurrentOpen)
        {
            var cssVars = $"--{CssVarPrefix}-height: 0px; --{CssVarPrefix}-width: 0px";
            userStyle = string.IsNullOrEmpty(userStyle) ? cssVars : $"{cssVars}; {userStyle}";
        }

        return AttributeUtilities.CombineStyles(AdditionalAttributes, userStyle);
    }

    private Dictionary<string, object> BuildAttributes(AccordionPanelState state)
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

        attributes["id"] = ResolvedId;
        attributes["role"] = "region";

        if (ItemContext?.TriggerId is not null)
            attributes["aria-labelledby"] = ItemContext.TriggerId;

        if (IsHidden)
        {
            if (ResolvedHiddenUntilFound)
                attributes["hidden"] = "until-found";
            else
                attributes["hidden"] = true;
        }

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }

    private async Task OpenAsync()
    {
        if (!hasRendered)
            return;

        try
        {
            var module = await moduleTask.Value;

            dotNetRef ??= DotNetObjectReference.Create(this);

            if (Element.HasValue)
            {
                if (!jsInitialized)
                {
                    await module.InvokeVoidAsync("initialize", Element.Value, dotNetRef, false, CssVarPrefix);
                    jsInitialized = true;
                }

                await module.InvokeVoidAsync("open", Element.Value, false);
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    private async Task CloseAsync()
    {
        if (!hasRendered)
            return;

        try
        {
            var module = await moduleTask.Value;
            if (Element.HasValue)
            {
                await module.InvokeVoidAsync("close", Element.Value);
            }
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (hasRendered && moduleTask.IsValueCreated)
        {
            try
            {
                var module = await moduleTask.Value;
                if (Element.HasValue)
                {
                    await module.InvokeVoidAsync("dispose", Element.Value);
                }
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }

        dotNetRef?.Dispose();
    }
}