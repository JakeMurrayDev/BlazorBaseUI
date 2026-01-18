using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Popover;

/// <summary>
/// A popover trigger component that can work either nested inside a PopoverRoot
/// or detached with a handle for typed payloads.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the popover. Use object for untyped payloads.</typeparam>
public class PopoverTypedTrigger<TPayload> : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "button";

    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;
    private string triggerId = null!;
    private PopoverTriggerState state;
    private CancellationTokenSource? hoverCts;
    private PopoverHandle<TPayload>? registeredHandle;

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
    public PopoverHandle<TPayload>? Handle { get; set; }

    [Parameter]
    public TPayload? Payload { get; set; }

    [Parameter]
    public Func<PopoverTriggerState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<PopoverTriggerState, string>? StyleValue { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public ElementReference? Element { get; private set; }

    private bool HasHandle => Handle is not null;

    private bool HasRootContext => RootContext is not null;

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

        // Handle Id change - unregister old trigger ID
        if (Id is not null && Id != triggerId)
        {
            UnregisterFromCurrentStore(triggerId);
            triggerId = Id;
        }

        // Handle change of handle parameter
        if (Handle != registeredHandle)
        {
            registeredHandle?.UnregisterTrigger(triggerId);
            registeredHandle = Handle;
            Handle?.RegisterTrigger(triggerId, Element, Payload);
        }
        else if (HasHandle)
        {
            // Update payload on existing handle
            Handle!.UpdateTriggerPayload(triggerId, Payload);
        }

        var open = IsOpenedByThisTrigger();
        state = new PopoverTriggerState(open, Disabled);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            if (HasHandle)
            {
                Handle!.RegisterTrigger(triggerId, Element, Payload);
                registeredHandle = Handle;
            }
            else
            {
                RootContext?.SetTriggerElement(Element);
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (RootContext is null && !HasHandle)
        {
            return;
        }

        var resolvedClass = AttributeUtilities.CombineClassNames(AdditionalAttributes, ClassValue?.Invoke(state));
        var resolvedStyle = AttributeUtilities.CombineStyles(AdditionalAttributes, StyleValue?.Invoke(state));
        var open = IsOpenedByThisTrigger();

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
        builder.AddAttribute(8, "id", triggerId);

        if (open)
        {
            builder.AddAttribute(9, "data-open", string.Empty);
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
                    OnElementChanged();
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
                    OnElementChanged();
                }
            });
            builder.CloseElement();
        }
    }

    public void Dispose()
    {
        CancelHoverDelay();
        UnregisterFromCurrentStore(triggerId);
    }

    private void OnElementChanged()
    {
        if (HasHandle)
        {
            Handle!.UpdateTriggerElement(triggerId, Element);
        }
        else
        {
            RootContext?.SetTriggerElement(Element);
        }
    }

    private void UnregisterFromCurrentStore(string id)
    {
        if (registeredHandle is not null)
        {
            registeredHandle.UnregisterTrigger(id);
        }
    }

    private bool IsOpenedByThisTrigger()
    {
        // Check handle state first if using detached trigger pattern
        if (HasHandle)
        {
            var isOpen = Handle!.IsOpen;
            if (!isOpen)
            {
                return false;
            }
            var activeId = Handle.ActiveTriggerId;
            return activeId == triggerId || (activeId is null && isOpen);
        }

        // Fall back to root context
        if (RootContext is null)
        {
            return false;
        }

        var rootOpen = RootContext.GetOpen();
        if (!rootOpen)
        {
            return false;
        }

        var activeTriggerId = RootContext.ActiveTriggerId;
        return activeTriggerId == triggerId || (activeTriggerId is null && rootOpen);
    }

    private async Task HandleClickAsync(MouseEventArgs e)
    {
        if (Disabled || (!HasRootContext && !HasHandle))
        {
            return;
        }

        var nextOpen = !IsOpenedByThisTrigger();
        await RequestOpenAsync(nextOpen, OpenChangeReason.TriggerPress);
        await EventUtilities.InvokeOnClickAsync(AdditionalAttributes, e);
    }

    private async Task HandleMouseEnterAsync(MouseEventArgs e)
    {
        if (Disabled || (!HasRootContext && !HasHandle) || !OpenOnHover)
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
                await RequestOpenAsync(true, OpenChangeReason.TriggerHover);
            }
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task HandleMouseLeaveAsync(MouseEventArgs e)
    {
        if (Disabled || (!HasRootContext && !HasHandle) || !OpenOnHover)
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
                await RequestOpenAsync(false, OpenChangeReason.TriggerHover);
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

    private Task RequestOpenAsync(bool open, OpenChangeReason reason)
    {
        if (HasHandle)
        {
            if (open)
            {
                Handle!.RequestOpen(triggerId, reason);
            }
            else
            {
                Handle!.RequestClose(reason);
            }
            return Task.CompletedTask;
        }

        if (RootContext is not null)
        {
            return RootContext.SetOpenAsync(open, reason, open ? Payload : null);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Non-generic version of PopoverTypedTrigger for scenarios where payload type is not needed.
/// </summary>
public sealed class PopoverTrigger : PopoverTypedTrigger<object?>
{
}
