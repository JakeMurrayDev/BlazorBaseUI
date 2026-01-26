using BlazorBaseUI.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorBaseUI.Menu;

public sealed class MenuSubmenuTrigger : ComponentBase, IReferencableComponent, IAsyncDisposable
{
    private const string DefaultTag = "div";

    private Lazy<Task<IJSObjectReference>>? moduleTask;
    private bool isComponentRenderAs;
    private bool hasRendered;
    private bool hoverInitialized;

    private Lazy<Task<IJSObjectReference>> ModuleTask => moduleTask ??= new Lazy<Task<IJSObjectReference>>(() =>
        JSRuntime!.InvokeAsync<IJSObjectReference>(
            "import", "./_content/BlazorBaseUI/blazor-baseui-menu.js").AsTask());

    [Inject]
    private IJSRuntime? JSRuntime { get; set; }

    [CascadingParameter]
    private MenuRootContext? RootContext { get; set; }

    [CascadingParameter]
    private MenuSubmenuRootContext? SubmenuContext { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool OpenOnHover { get; set; } = true;

    [Parameter]
    public int Delay { get; set; } = 100;

    [Parameter]
    public int CloseDelay { get; set; }

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public Func<MenuSubmenuTriggerState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<MenuSubmenuTriggerState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        if (SubmenuContext is null)
        {
            throw new InvalidOperationException("MenuSubmenuTrigger must be placed inside a MenuSubmenuRoot.");
        }
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            hasRendered = true;
            RootContext?.SetTriggerElement(Element);

            if (OpenOnHover && !hoverInitialized)
            {
                _ = InitializeHoverInteractionAsync();
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null)
        {
            return;
        }

        var open = RootContext.GetOpen();
        var highlighted = GetHighlighted();
        var disabled = Disabled || RootContext.Disabled;

        var state = new MenuSubmenuTriggerState(
            Disabled: disabled,
            Highlighted: highlighted,
            Open: open);

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            RenderAttributes(builder, resolvedClass, resolvedStyle, open, highlighted, disabled);
            builder.AddComponentParameter(17, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(18, component => Element = ((IReferencableComponent)component).Element);
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            RenderAttributes(builder, resolvedClass, resolvedStyle, open, highlighted, disabled);
            builder.AddContent(17, ChildContent);
            builder.AddElementReferenceCapture(18, elementReference => Element = elementReference);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (moduleTask?.IsValueCreated == true && hasRendered && hoverInitialized && RootContext is not null)
        {
            try
            {
                var module = await ModuleTask.Value;
                await module.InvokeVoidAsync("disposeHoverInteraction", RootContext.RootId);
            }
            catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
            {
            }
        }
    }

    private void RenderAttributes(RenderTreeBuilder builder, string? resolvedClass, string? resolvedStyle, bool open, bool highlighted, bool disabled)
    {
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttribute(2, "role", "menuitem");
        builder.AddAttribute(3, "aria-haspopup", "menu");
        builder.AddAttribute(4, "aria-expanded", open ? "true" : "false");
        builder.AddAttribute(5, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

        if (disabled)
        {
            builder.AddAttribute(6, "aria-disabled", "true");
        }

        builder.AddAttribute(7, "tabindex", open || highlighted ? 0 : -1);

        if (open)
        {
            builder.AddAttribute(8, "data-open", string.Empty);
            builder.AddAttribute(9, "data-popup-open", string.Empty);
        }
        else
        {
            builder.AddAttribute(10, "data-closed", string.Empty);
        }

        if (disabled)
        {
            builder.AddAttribute(11, "data-disabled", string.Empty);
        }

        if (highlighted)
        {
            builder.AddAttribute(12, "data-highlighted", string.Empty);
        }

        if (!string.IsNullOrEmpty(Label))
        {
            builder.AddAttribute(13, "data-label", Label);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(14, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(15, "style", resolvedStyle);
        }
    }

    private bool GetHighlighted()
    {
        return SubmenuContext?.ParentMenu?.ActiveIndex >= 0;
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (Disabled || RootContext is null)
        {
            return;
        }

        var nextOpen = !RootContext.GetOpen();
        await RootContext.SetOpenAsync(nextOpen, OpenChangeReason.TriggerPress, null);
        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }

    private async Task InitializeHoverInteractionAsync()
    {
        if (RootContext is null || !Element.HasValue)
        {
            return;
        }

        try
        {
            var module = await ModuleTask.Value;
            var effectiveCloseDelay = CloseDelay > 0 ? CloseDelay : Delay;
            await module.InvokeVoidAsync("initializeHoverInteraction", RootContext.RootId, Element.Value, Delay, effectiveCloseDelay);
            hoverInitialized = true;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or TaskCanceledException)
        {
        }
    }
}
