using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorBaseUI.Dialog;

/// <summary>
/// A dialog trigger component that can work either nested inside a DialogRoot
/// or detached with a handle for typed payloads.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the dialog. Use object for untyped payloads.</typeparam>
public class DialogTypedTrigger<TPayload> : ComponentBase, IReferencableComponent, IDisposable
{
    private const string DefaultTag = "button";

    private bool isComponentRenderAs;
    private IReferencableComponent? componentReference;
    private string triggerId = null!;
    private DialogTriggerState state;
    private DialogHandle<TPayload>? registeredHandle;

    private bool HasHandle => Handle is not null;

    private bool HasRootContext => RootContext is not null;

    [CascadingParameter]
    private DialogRootContext? RootContext { get; set; }

    [Parameter]
    public string? As { get; set; }

    [Parameter]
    public Type? RenderAs { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public bool NativeButton { get; set; } = true;

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public DialogHandle<TPayload>? Handle { get; set; }

    [Parameter]
    public TPayload? Payload { get; set; }

    [Parameter]
    public Func<DialogTriggerState, string>? ClassValue { get; set; }

    [Parameter]
    public Func<DialogTriggerState, string>? StyleValue { get; set; }

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
        state = new DialogTriggerState(Disabled, open);

        // Update payload via context if not using handle
        if (!HasHandle)
        {
            RootContext?.SetTriggerPayload(triggerId, Payload);
        }
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
                RootContext?.RegisterTriggerElement(triggerId, Element);
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
        var isNativeButton = NativeButton && (string.IsNullOrEmpty(As) || As == "button");

        if (isComponentRenderAs)
        {
            builder.OpenRegion(0);
            builder.OpenComponent(0, RenderAs!);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "aria-haspopup", "dialog");
            builder.AddAttribute(3, "aria-expanded", open ? "true" : "false");
            builder.AddAttribute(4, "id", triggerId);

            if (isNativeButton)
            {
                builder.AddAttribute(5, "type", "button");
                if (Disabled)
                {
                    builder.AddAttribute(6, "disabled", true);
                }
            }
            else
            {
                builder.AddAttribute(7, "role", "button");
                builder.AddAttribute(8, "tabindex", Disabled ? "-1" : "0");
                if (Disabled)
                {
                    builder.AddAttribute(9, "aria-disabled", "true");
                }
            }

            if (open)
            {
                builder.AddAttribute(10, "data-popup-open", string.Empty);
            }

            if (Disabled)
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

            if (!isNativeButton)
            {
                builder.AddAttribute(15, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));
            }

            builder.AddAttribute(16, "ChildContent", ChildContent);
            builder.AddComponentReferenceCapture(17, component =>
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
            builder.CloseRegion();
        }
        else
        {
            builder.OpenRegion(1);
            builder.OpenElement(0, !string.IsNullOrEmpty(As) ? As : DefaultTag);
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "aria-haspopup", "dialog");
            builder.AddAttribute(3, "aria-expanded", open ? "true" : "false");
            builder.AddAttribute(4, "id", triggerId);

            if (isNativeButton)
            {
                builder.AddAttribute(5, "type", "button");
                if (Disabled)
                {
                    builder.AddAttribute(6, "disabled", true);
                }
            }
            else
            {
                builder.AddAttribute(7, "role", "button");
                builder.AddAttribute(8, "tabindex", Disabled ? "-1" : "0");
                if (Disabled)
                {
                    builder.AddAttribute(9, "aria-disabled", "true");
                }
            }

            if (open)
            {
                builder.AddAttribute(10, "data-popup-open", string.Empty);
            }

            if (Disabled)
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

            if (!isNativeButton)
            {
                builder.AddAttribute(15, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));
            }

            builder.AddContent(16, ChildContent);
            builder.AddElementReferenceCapture(17, elementReference =>
            {
                if (!Nullable.Equals(Element, elementReference))
                {
                    Element = elementReference;
                    OnElementChanged();
                }
            });
            builder.CloseElement();
            builder.CloseRegion();
        }
    }

    public void Dispose()
    {
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
            RootContext?.RegisterTriggerElement(triggerId, Element);
        }
    }

    private void UnregisterFromCurrentStore(string id)
    {
        if (registeredHandle is not null)
        {
            registeredHandle.UnregisterTrigger(id);
        }
        else
        {
            RootContext?.UnregisterTriggerElement(id);
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

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (Disabled || (!HasRootContext && !HasHandle))
        {
            return;
        }

        // Handle Enter and Space keys for non-native buttons
        if (e.Key == "Enter" || e.Key == " ")
        {
            var nextOpen = !IsOpenedByThisTrigger();
            await RequestOpenAsync(nextOpen, OpenChangeReason.TriggerPress);
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
            if (open)
            {
                return RootContext.SetOpenWithTriggerIdAsync(triggerId, Payload, reason);
            }
            else
            {
                return RootContext.SetOpenAsync(false, reason);
            }
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Non-generic version of DialogTypedTrigger for scenarios where payload type is not needed.
/// </summary>
public sealed class DialogTrigger : DialogTypedTrigger<object?>
{
}
