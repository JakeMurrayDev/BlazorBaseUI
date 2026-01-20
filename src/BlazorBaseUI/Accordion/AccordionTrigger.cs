using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Accordion;

public sealed class AccordionTrigger : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "button";
    private const string JsModulePath = "./_content/BlazorBaseUI/blazor-baseui-accordion-trigger.js";

    private readonly Lazy<Task<IJSObjectReference>> moduleTask;

    private string? defaultId;
    private bool isComponentRenderAs;
    private AccordionTriggerState state = new(false, Orientation.Vertical, string.Empty, false);

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

    public ElementReference? Element { get; private set; }

    private bool ResolvedDisabled => Disabled ?? ItemContext?.Disabled ?? false;

    private string ResolvedId
    {
        get
        {
            var id = AttributeUtilities.GetIdOrDefault(AdditionalAttributes, () => defaultId ??= Guid.NewGuid().ToIdString());
            if (id != ItemContext?.TriggerId)
            {
                ItemContext?.SetTriggerId(id);
            }
            return id;
        }
    }

    public AccordionTrigger()
    {
        moduleTask = new Lazy<Task<IJSObjectReference>>(() =>
            JSRuntime.InvokeAsync<IJSObjectReference>("import", JsModulePath).AsTask());
    }

    protected override void OnInitialized()
    {
        state = new AccordionTriggerState(
            ItemContext?.Open ?? false,
            ItemContext?.Orientation ?? Orientation.Vertical,
            ItemContext?.StringValue ?? string.Empty,
            ResolvedDisabled);
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var currentOpen = ItemContext?.Open ?? false;
        var currentOrientation = ItemContext?.Orientation ?? Orientation.Vertical;
        var currentValue = ItemContext?.StringValue ?? string.Empty;
        var currentDisabled = ResolvedDisabled;

        if (state.Open != currentOpen || state.Orientation != currentOrientation || state.Value != currentValue || state.Disabled != currentDisabled)
        {
            state = state with { Open = currentOpen, Orientation = currentOrientation, Value = currentValue, Disabled = currentDisabled };
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (Element.HasValue)
            {
                try
                {
                    var module = await moduleTask.Value;
                    var isHorizontal = RootContext?.Orientation == Orientation.Horizontal;
                    var isRtl = RootContext?.Direction == Direction.Rtl;
                    var loopFocus = RootContext?.LoopFocus ?? true;

                    await module.InvokeVoidAsync("initialize", Element.Value, isHorizontal, isRtl, loopFocus);
                }
                catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
                {
                }
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ItemContext is null)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

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

        if (NativeButton)
        {
            builder.AddAttribute(3, "type", "button");
        }

        builder.AddAttribute(4, "tabindex", 0);
        builder.AddAttribute(5, "aria-disabled", ResolvedDisabled ? "true" : "false");
        builder.AddAttribute(6, "aria-expanded", ItemContext.Open ? "true" : "false");

        builder.AddAttribute(7, "aria-controls", ItemContext.PanelId);

        if (ResolvedDisabled)
        {
            builder.AddAttribute(8, "disabled", true);
        }

        builder.AddAttribute(9, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

        builder.AddAttribute(10, "data-value", state.Value);
        builder.AddAttribute(11, "data-orientation", state.Orientation.ToDataAttributeString());

        if (state.Open)
        {
            builder.AddAttribute(12, "data-panel-open", string.Empty);
        }

        if (state.Disabled)
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
            builder.AddAttribute(16, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(17, component => { Element = ((IReferencableComponent)component).Element; });
            builder.CloseComponent();
        }
        else
        {
            builder.AddElementReferenceCapture(18, elementReference => Element = elementReference);
            builder.AddContent(19, ChildContent);
            builder.CloseElement();
        }
    }

    private async Task HandleClickAsync(MouseEventArgs args)
    {
        if (ResolvedDisabled)
        {
            return;
        }

        ItemContext?.HandleTrigger();
        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, args);
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
    }
}
