using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Popover;

public sealed class PopoverTrigger : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "button";

    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;
    private PopoverTriggerState state;
    private CancellationTokenSource? hoverCts;

    [CascadingParameter]
    private PopoverRootContext? RootContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool NativeButton { get; set; } = true;

    [Parameter]
    public bool OpenOnHover { get; set; }

    [Parameter]
    public int Delay { get; set; } = 300;

    [Parameter]
    public int CloseDelay { get; set; }

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public Func<PopoverTriggerState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<PopoverTriggerState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        var open = RootContext?.GetOpen() ?? false;
        state = new PopoverTriggerState(open, Disabled);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            RootContext?.SetTriggerElement(Element);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var open = RootContext.GetOpen();

        if (isComponentRenderAs)
        {
            builder.OpenComponent(0, RenderAs!);
        }
        else
        {
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
        }

        builder.AddMultipleAttributes(1, AdditionalAttributes);

        if (!NativeButton && string.IsNullOrEmpty(As))
        {
            builder.AddAttribute(2, "role", "button");
            if (Disabled)
            {
                builder.AddAttribute(3, "aria-disabled", "true");
            }
        }

        if (NativeButton || string.IsNullOrEmpty(As) || As == "button")
        {
            builder.AddAttribute(4, "type", "button");
            if (Disabled)
            {
                builder.AddAttribute(5, "disabled", true);
            }
        }

        builder.AddAttribute(6, "aria-haspopup", "dialog");
        builder.AddAttribute(7, "aria-expanded", open ? "true" : "false");

        if (!string.IsNullOrEmpty(Id))
        {
            builder.AddAttribute(8, "id", Id);
        }

        if (open)
        {
            builder.AddAttribute(9, "data-popup-open", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(10, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(11, "style", resolvedStyle);
        }

        builder.AddAttribute(12, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

        if (OpenOnHover)
        {
            builder.AddAttribute(13, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnterAsync));
            builder.AddAttribute(14, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseLeaveAsync));
        }

        if (isComponentRenderAs)
        {
            builder.AddAttribute(15, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(16, component =>
            {
                componentReference = (IReferencableComponent)component;
                var newElement = componentReference.Element;
                if (!Nullable.Equals(Element, newElement))
                {
                    Element = newElement;
                    RootContext?.SetTriggerElement(Element);
                }
            });
            builder.CloseComponent();
        }
        else
        {
            builder.AddContent(17, ChildContent);
            builder.AddElementReferenceCapture(18, elementReference =>
            {
                if (!Nullable.Equals(Element, elementReference))
                {
                    Element = elementReference;
                    RootContext?.SetTriggerElement(Element);
                }
            });
            builder.CloseElement();
        }
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (Disabled || RootContext is null)
        {
            return;
        }

        var nextOpen = !RootContext.GetOpen();
        await RootContext.SetOpenAsync(nextOpen, OpenChangeReason.TriggerPress);
        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }

    private async Task HandleMouseEnterAsync(MouseEventArgs e)
    {
        if (Disabled || RootContext is null || !OpenOnHover)
        {
            return;
        }

        CancelHoverDelay();

        if (RootContext.GetOpen())
        {
            return;
        }

        hoverCts = new CancellationTokenSource();
        var token = hoverCts.Token;

        try
        {
            await Task.Delay(Delay, token);
            if (!token.IsCancellationRequested)
            {
                await RootContext.SetOpenAsync(true, OpenChangeReason.TriggerHover);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task HandleMouseLeaveAsync(MouseEventArgs e)
    {
        if (Disabled || RootContext is null || !OpenOnHover)
        {
            return;
        }

        CancelHoverDelay();

        if (!RootContext.GetOpen())
        {
            return;
        }

        hoverCts = new CancellationTokenSource();
        var token = hoverCts.Token;

        try
        {
            var effectiveCloseDelay = CloseDelay > 0 ? CloseDelay : Delay;
            await Task.Delay(effectiveCloseDelay, token);
            if (!token.IsCancellationRequested)
            {
                await RootContext.SetOpenAsync(false, OpenChangeReason.TriggerHover);
            }
        }
        catch (TaskCanceledException)
        {
        }
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

    public void Dispose()
    {
        CancelHoverDelay();
    }
}
