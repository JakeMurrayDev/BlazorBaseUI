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
    public ElementReference? Element => element;

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

        var state = State;
        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var attributes = BuildAttributes(state);

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
        attributes["aria-disabled"] = ResolvedDisabled ? "true" : "false";
        attributes["aria-expanded"] = ItemContext?.Open == true ? "true" : "false";

        if (ItemContext?.Open is true)
            attributes["aria-controls"] = ItemContext.PanelId;

        if (ResolvedDisabled)
            attributes["disabled"] = true;

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

public record AccordionTriggerState(bool Open, Orientation Orientation, string Value, bool Disabled)
{
    public Dictionary<string, object> GetDataAttributes()
    {
        var attributes = new Dictionary<string, object>
        {
            [AccordionTriggerDataAttribute.Value.ToDataAttributeString()] = Value,
            [AccordionTriggerDataAttribute.Orientation.ToDataAttributeString()] = Orientation.ToDataAttributeString()!
        };

        if (Open)
            attributes[AccordionTriggerDataAttribute.PanelOpen.ToDataAttributeString()] = string.Empty;

        if (Disabled)
            attributes[AccordionTriggerDataAttribute.Disabled.ToDataAttributeString()] = string.Empty;

        return attributes;
    }
}