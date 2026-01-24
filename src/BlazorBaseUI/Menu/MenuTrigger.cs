using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Menu;

public sealed class MenuTrigger : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "button";

    private bool isComponentRenderAs;
    private string triggerId = null!;
    private MenuTriggerState state;
    private CancellationTokenSource? hoverCts;

    [CascadingParameter]
    private MenuRootContext? RootContext { get; set; }

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
    public int Delay { get; set; } = 100;

    [Parameter]
    public int CloseDelay { get; set; }

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public Func<MenuTriggerState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<MenuTriggerState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    protected override void OnInitialized()
    {
        triggerId = Id ?? Guid.NewGuid().ToIdString();
    }

    protected override void OnParametersSet()
    {
        isComponentRenderAs = RenderAs is not null;
        if (isComponentRenderAs && !typeof(IReferencableComponent).IsAssignableFrom(RenderAs))
        {
            throw new InvalidOperationException($"Type {RenderAs!.Name} must implement IReferencableComponent.");
        }

        if (Id is not null && Id != triggerId)
        {
            triggerId = Id;
        }

        var open = IsOpenedByThisTrigger();
        var disabled = Disabled || (RootContext?.Disabled ?? false);
        state = new MenuTriggerState(open, disabled);
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
        var open = IsOpenedByThisTrigger();
        var disabled = state.Disabled;

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            RenderAttributes(builder, resolvedClass, resolvedStyle, open, disabled);
            builder.AddAttribute(17, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(18, component =>
            {
                var newElement = ((IReferencableComponent)component).Element;
                if (!Nullable.Equals(Element, newElement))
                {
                    Element = newElement;
                    RootContext?.SetTriggerElement(Element);
                }
            });
            builder.CloseComponent();
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            RenderAttributes(builder, resolvedClass, resolvedStyle, open, disabled);
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
            builder.CloseRegion();
        }
    }

    public void Dispose()
    {
        CancelHoverDelay();
    }

    private void RenderAttributes(RenderTreeBuilder builder, string? resolvedClass, string? resolvedStyle, bool open, bool disabled)
    {
        builder.AddMultipleAttributes(1, AdditionalAttributes);

        if (!NativeButton && string.IsNullOrEmpty(As))
        {
            builder.AddAttribute(2, "role", "button");
            if (disabled)
            {
                builder.AddAttribute(3, "aria-disabled", "true");
            }
        }

        if (NativeButton || string.IsNullOrEmpty(As) || As == "button")
        {
            builder.AddAttribute(4, "type", "button");
            if (disabled)
            {
                builder.AddAttribute(5, "disabled", true);
            }
        }

        builder.AddAttribute(6, "aria-haspopup", "menu");
        builder.AddAttribute(7, "aria-expanded", open ? "true" : "false");
        builder.AddAttribute(8, "id", triggerId);

        if (open)
        {
            builder.AddAttribute(9, "data-popup-open", string.Empty);

            var openReason = RootContext!.OpenChangeReason;
            if (openReason == OpenChangeReason.TriggerPress)
            {
                builder.AddAttribute(10, "data-pressed", string.Empty);
            }
        }

        if (disabled)
        {
            builder.AddAttribute(11, "data-disabled", string.Empty);
        }

        if (!string.IsNullOrEmpty(resolvedClass))
        {
            builder.AddAttribute(12, "class", resolvedClass);
        }

        if (!string.IsNullOrEmpty(resolvedStyle))
        {
            builder.AddAttribute(13, "style", resolvedStyle);
        }

        builder.AddAttribute(14, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

        if (OpenOnHover)
        {
            builder.AddAttribute(15, "onmouseenter", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnterAsync));
            builder.AddAttribute(16, "onmouseleave", EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseLeaveAsync));
        }
    }

    private bool IsOpenedByThisTrigger()
    {
        if (RootContext is null)
        {
            return false;
        }

        return RootContext.GetOpen();
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (state.Disabled || RootContext is null)
        {
            return;
        }

        var nextOpen = !IsOpenedByThisTrigger();
        await RootContext.SetOpenAsync(nextOpen, OpenChangeReason.TriggerPress, null);
        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }

    private async Task HandleMouseEnterAsync(MouseEventArgs e)
    {
        if (state.Disabled || RootContext is null || !OpenOnHover)
        {
            return;
        }

        CancelHoverDelay();

        if (IsOpenedByThisTrigger())
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
                await RootContext.SetOpenAsync(true, OpenChangeReason.TriggerHover, null);
            }
        }
        catch (TaskCanceledException)
        {
        }

        await EventUtilities.InvokeOnMouseEnterAsync(AdditionalAttributes, e);
    }

    private async Task HandleMouseLeaveAsync(MouseEventArgs e)
    {
        if (state.Disabled || RootContext is null || !OpenOnHover)
        {
            return;
        }

        CancelHoverDelay();

        if (!IsOpenedByThisTrigger())
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
                await RootContext.SetOpenAsync(false, OpenChangeReason.TriggerHover, null);
            }
        }
        catch (TaskCanceledException)
        {
        }

        await EventUtilities.InvokeOnMouseLeaveAsync(AdditionalAttributes, e);
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
