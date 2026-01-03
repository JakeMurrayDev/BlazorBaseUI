using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Accordion;

public sealed class AccordionTrigger : ComponentBase, IAsyncDisposable
{
    private const string DefaultTag = "button";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-accordion-trigger.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private string? defaultId;
    private bool hasRendered;
    private ElementReference element;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    [CascadingParameter]
    private IAccordionRootContext? RootContext { get; set; }

    [CascadingParameter]
    private IAccordionItemContext? ItemContext { get; set; }

    [Parameter]
    public bool? Disabled { get; set; }

    [Parameter]
    public bool NativeButton { get; set; } = true;

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<AccordionTriggerState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<AccordionTriggerState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [DisallowNull]
    public ElementReference? Element { get; private set; }

    private bool ResolvedDisabled => Disabled ?? ItemContext?.Disabled ?? false;

    private string ResolvedId => AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());

    private AccordionTriggerState State => new(
        ItemContext?.Open ?? false,
        ItemContext?.Orientation ?? Orientation.Vertical,
        ItemContext?.StringValue ?? string.Empty,
        ResolvedDisabled);

    public AccordionTrigger()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        ItemContext?.SetTriggerId(ResolvedId);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;

            try
            {
                var module = await moduleTask.Value;
                var isHorizontal = RootContext?.Orientation == Orientation.Horizontal;
                var isRtl = RootContext?.Direction == Direction.Rtl;
                var loopFocus = RootContext?.LoopFocus ?? true;

                await module.InvokeVoidAsync("initialize", element, isHorizontal, isRtl, loopFocus);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ItemContext is null)
            return;
        
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(State));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(State));
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
        builder.AddElementReferenceCapture(5, e => element = e);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    private Dictionary<string, object> BuildAttributes(AccordionTriggerState state)
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

        if (NativeButton)
            attributes["type"] = "button";

        attributes["tabindex"] = 0;
        attributes["aria-Disabled"] = ResolvedDisabled ? "true" : "false";
        attributes["aria-expanded"] = ItemContext?.Open == true ? "true" : "false";

        if (ItemContext?.Open is true)
            attributes["aria-controls"] = ItemContext.PanelId;

        if (ResolvedDisabled)
            attributes["Disabled"] = true;

        attributes["onclick"] = EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick);

        foreach (var dataAttr in state.GetDataAttributes())
            attributes[dataAttr.Key] = dataAttr.Value;

        return attributes;
    }

    private void HandleClick(MouseEventArgs args)
    {
        if (ResolvedDisabled)
            return;

        ItemContext?.HandleTrigger();
    }

    public async ValueTask DisposeAsync()
    {
        if (hasRendered && moduleTask.IsValueCreated)
        {
            try
            {
                var module = await moduleTask.Value;
                await module.InvokeVoidAsync("dispose", element);
                await module.DisposeAsync();
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }
}