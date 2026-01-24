using BlazorBaseUI.Utilities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Menu;

public sealed class MenuSubmenuTrigger : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "div";

    private bool isComponentRenderAs;
    private CancellationTokenSource? hoverCts;

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
            builder.AddComponentParameter(18, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(19, component => Element = ((IReferencableComponent)component).Element);
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            RenderAttributes(builder, resolvedClass, resolvedStyle, open, highlighted, disabled);
            builder.AddContent(18, ChildContent);
            builder.AddElementReferenceCapture(19, elementReference => Element = elementReference);
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    public void Dispose()
    {
        CancelHoverDelay();
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
        builder.AddAttribute(8, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnterAsync));
        builder.AddAttribute(9, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseLeaveAsync));
        builder.AddAttribute(10, "onblur", EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlurAsync));

        if (open)
        {
            builder.AddAttribute(11, "data-open", string.Empty);
        }
        else
        {
            builder.AddAttribute(12, "data-closed", string.Empty);
        }

        if (disabled)
        {
            builder.AddAttribute(13, "data-disabled", string.Empty);
        }

        if (highlighted)
        {
            builder.AddAttribute(14, "data-highlighted", string.Empty);
        }

        if (!string.IsNullOrEmpty(Label))
        {
            builder.AddAttribute(15, "data-label", Label);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(16, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(17, "style", resolvedStyle);
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

    [SlopwatchSuppress("SW003", "TaskCanceledException is expected when hover delay is cancelled by user mouse movement")]
    private async Task HandleMouseEnterAsync(MouseEventArgs e)
    {
        if (Disabled || RootContext is null)
        {
            return;
        }

        CancelHoverDelay();

        if (OpenOnHover && !RootContext.GetOpen())
        {
            hoverCts = new CancellationTokenSource();
            var token = hoverCts.Token;

            try
            {
                await Task.Delay(Delay, token);
                if (!token.IsCancellationRequested)
                {
                    await RootContext.SetOpenAsync(true, OpenChangeReason.TriggerHover, null);
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        await EventUtilities.InvokeOnMouseEnterAsync(AdditionalAttributes, e);
    }

    [SlopwatchSuppress("SW003", "TaskCanceledException is expected when hover delay is cancelled by user mouse movement")]
    private async Task HandleMouseLeaveAsync(MouseEventArgs e)
    {
        if (Disabled || RootContext is null)
        {
            return;
        }

        CancelHoverDelay();

        if (OpenOnHover && RootContext.GetOpen())
        {
            hoverCts = new CancellationTokenSource();
            var token = hoverCts.Token;

            try
            {
                var effectiveCloseDelay = CloseDelay > 0 ? CloseDelay : Delay;
                await Task.Delay(effectiveCloseDelay, token);
                if (!token.IsCancellationRequested)
                {
                    await RootContext.SetOpenAsync(false, OpenChangeReason.TriggerHover, null);
                }
            }
            catch (TaskCanceledException)
            {
            }
        }

        await EventUtilities.InvokeOnMouseLeaveAsync(AdditionalAttributes, e);
    }

    private async Task HandleBlurAsync(FocusEventArgs e)
    {
        await EventUtilities.InvokeOnBlurAsync(AdditionalAttributes, e);
    }

    private void CancelHoverDelay()
    {
        if (hoverCts is not null)
        {
            hoverCts.Cancel();
            hoverCts.Dispose();
            hoverCts = null;
        }
    }
}
