using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Dialog;

/// <summary>
/// Non-generic interface for DialogHandle that allows DialogRoot to interact with handles
/// without knowing the payload type at compile time.
/// </summary>
public interface IDialogHandle
{
    /// <summary>
    /// Gets a value indicating whether the dialog is currently open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets the ID of the currently active trigger.
    /// </summary>
    string? ActiveTriggerId { get; }

    /// <summary>
    /// Opens the dialog and associates it with the trigger with the given ID.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the dialog.</param>
    void Open(string triggerId);

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    void Close();

    /// <summary>
    /// Gets the element reference for a trigger.
    /// </summary>
    internal ElementReference? GetTriggerElement(string? triggerId);

    /// <summary>
    /// Gets the payload for a trigger as an object.
    /// </summary>
    internal object? GetTriggerPayloadAsObject(string? triggerId);

    /// <summary>
    /// Subscribes a component to handle state changes.
    /// </summary>
    internal void Subscribe(IDialogHandleSubscriber subscriber);

    /// <summary>
    /// Unsubscribes a component from handle state changes.
    /// </summary>
    internal void Unsubscribe(IDialogHandleSubscriber subscriber);

    /// <summary>
    /// Called by root to sync state back to handle after processing.
    /// </summary>
    internal void SyncState(bool open, string? triggerId, object? payload);
}

/// <summary>
/// A handle to control a dialog imperatively and to associate detached triggers with it.
/// The handle owns the dialog state and coordinates between detached Root and Trigger components.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the dialog.</typeparam>
public class DialogHandle<TPayload> : IDialogHandle
{
    private readonly Dictionary<string, TriggerData> registeredTriggers = [];
    private readonly List<IDialogHandleSubscriber> subscribers = [];

    private bool isOpen;
    private string? activeTriggerId;
    private TPayload? payload;

    /// <summary>
    /// Gets a value indicating whether the dialog is currently open.
    /// </summary>
    public bool IsOpen => isOpen;

    /// <summary>
    /// Gets the ID of the currently active trigger.
    /// </summary>
    public string? ActiveTriggerId => activeTriggerId;

    /// <summary>
    /// Gets the current payload value.
    /// </summary>
    public TPayload? Payload => payload;

    /// <summary>
    /// Opens the dialog and associates it with the trigger with the given ID.
    /// The trigger must be a DialogTrigger component with this handle passed as a prop.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the dialog.</param>
    /// <exception cref="InvalidOperationException">Thrown when no trigger is found with the given ID.</exception>
    public void Open(string triggerId)
    {
        if (string.IsNullOrEmpty(triggerId))
        {
            throw new ArgumentException("Trigger ID cannot be null or empty.", nameof(triggerId));
        }

        if (!registeredTriggers.ContainsKey(triggerId))
        {
            throw new InvalidOperationException($"DialogHandle.Open: No trigger found with id \"{triggerId}\".");
        }

        SetOpenInternal(true, OpenChangeReason.ImperativeAction, triggerId);
    }

    /// <summary>
    /// Closes the dialog.
    /// </summary>
    public void Close()
    {
        SetOpenInternal(false, OpenChangeReason.ImperativeAction, null);
    }

    /// <summary>
    /// Registers a trigger with this handle.
    /// </summary>
    internal void RegisterTrigger(string triggerId, ElementReference? element, TPayload? triggerPayload)
    {
        registeredTriggers[triggerId] = new TriggerData(element, triggerPayload);

        foreach (var subscriber in subscribers.ToArray())
        {
            subscriber.OnTriggerRegistered(triggerId, element);
        }
    }

    /// <summary>
    /// Unregisters a trigger from this handle.
    /// </summary>
    internal void UnregisterTrigger(string triggerId)
    {
        registeredTriggers.Remove(triggerId);

        foreach (var subscriber in subscribers.ToArray())
        {
            subscriber.OnTriggerUnregistered(triggerId);
        }
    }

    /// <summary>
    /// Updates the element reference for a trigger.
    /// </summary>
    internal void UpdateTriggerElement(string triggerId, ElementReference? element)
    {
        if (registeredTriggers.TryGetValue(triggerId, out var data))
        {
            registeredTriggers[triggerId] = data with { Element = element };

            foreach (var subscriber in subscribers.ToArray())
            {
                subscriber.OnTriggerElementUpdated(triggerId, element);
            }
        }
    }

    /// <summary>
    /// Updates the payload for a trigger.
    /// </summary>
    internal void UpdateTriggerPayload(string triggerId, TPayload? triggerPayload)
    {
        if (registeredTriggers.TryGetValue(triggerId, out var data))
        {
            registeredTriggers[triggerId] = data with { Payload = triggerPayload };

            if (activeTriggerId == triggerId && isOpen)
            {
                payload = triggerPayload;
                NotifyStateChanged();
            }
        }
    }

    /// <summary>
    /// Gets the element reference for a trigger.
    /// </summary>
    internal ElementReference? GetTriggerElement(string? triggerId)
    {
        if (triggerId is not null && registeredTriggers.TryGetValue(triggerId, out var data))
        {
            return data.Element;
        }

        return null;
    }

    /// <summary>
    /// Gets the payload for a trigger.
    /// </summary>
    internal TPayload? GetTriggerPayload(string? triggerId)
    {
        if (triggerId is not null && registeredTriggers.TryGetValue(triggerId, out var data))
        {
            return data.Payload;
        }

        return default;
    }

    /// <summary>
    /// Checks if a trigger is registered with this handle.
    /// </summary>
    internal bool HasTrigger(string triggerId)
    {
        return registeredTriggers.ContainsKey(triggerId);
    }

    /// <summary>
    /// Subscribes a component to handle state changes.
    /// </summary>
    internal void Subscribe(IDialogHandleSubscriber subscriber)
    {
        if (!subscribers.Contains(subscriber))
        {
            subscribers.Add(subscriber);
        }
    }

    /// <summary>
    /// Unsubscribes a component from handle state changes.
    /// </summary>
    internal void Unsubscribe(IDialogHandleSubscriber subscriber)
    {
        subscribers.Remove(subscriber);
    }

    /// <summary>
    /// Called by triggers to request opening the dialog.
    /// </summary>
    internal void RequestOpen(string triggerId, OpenChangeReason reason)
    {
        SetOpenInternal(true, reason, triggerId);
    }

    /// <summary>
    /// Called by triggers to request closing the dialog.
    /// </summary>
    internal void RequestClose(OpenChangeReason reason)
    {
        SetOpenInternal(false, reason, null);
    }

    /// <summary>
    /// Called by root to sync state back to handle after processing.
    /// </summary>
    internal void SyncState(bool open, string? triggerId, TPayload? currentPayload)
    {
        isOpen = open;
        activeTriggerId = triggerId;
        payload = currentPayload;
    }

    /// <inheritdoc />
    ElementReference? IDialogHandle.GetTriggerElement(string? triggerId)
    {
        return GetTriggerElement(triggerId);
    }

    /// <inheritdoc />
    object? IDialogHandle.GetTriggerPayloadAsObject(string? triggerId)
    {
        return GetTriggerPayload(triggerId);
    }

    /// <inheritdoc />
    void IDialogHandle.Subscribe(IDialogHandleSubscriber subscriber)
    {
        Subscribe(subscriber);
    }

    /// <inheritdoc />
    void IDialogHandle.Unsubscribe(IDialogHandleSubscriber subscriber)
    {
        Unsubscribe(subscriber);
    }

    /// <inheritdoc />
    void IDialogHandle.SyncState(bool open, string? triggerId, object? payload)
    {
        SyncState(open, triggerId, payload is TPayload typedPayload ? typedPayload : default);
    }

    private void SetOpenInternal(bool nextOpen, OpenChangeReason reason, string? triggerId)
    {
        if (isOpen == nextOpen && (nextOpen == false || activeTriggerId == triggerId))
        {
            return;
        }

        if (nextOpen && triggerId is not null)
        {
            activeTriggerId = triggerId;
            payload = GetTriggerPayload(triggerId);
        }

        foreach (var subscriber in subscribers.ToArray())
        {
            subscriber.OnOpenChangeRequested(nextOpen, reason, triggerId);
        }
    }

    private void NotifyStateChanged()
    {
        foreach (var subscriber in subscribers.ToArray())
        {
            subscriber.OnStateChanged();
        }
    }

    private readonly record struct TriggerData(ElementReference? Element, TPayload? Payload);
}

/// <summary>
/// Non-generic version of DialogHandle for scenarios where payload type is not needed.
/// </summary>
public sealed class DialogHandle : DialogHandle<object?>
{
}

/// <summary>
/// Interface for components that subscribe to DialogHandle state changes.
/// </summary>
internal interface IDialogHandleSubscriber
{
    /// <summary>
    /// Called when a trigger is registered with the handle.
    /// </summary>
    void OnTriggerRegistered(string triggerId, ElementReference? element);

    /// <summary>
    /// Called when a trigger is unregistered from the handle.
    /// </summary>
    void OnTriggerUnregistered(string triggerId);

    /// <summary>
    /// Called when a trigger's element reference is updated.
    /// </summary>
    void OnTriggerElementUpdated(string triggerId, ElementReference? element);

    /// <summary>
    /// Called when an open/close state change is requested.
    /// </summary>
    void OnOpenChangeRequested(bool open, OpenChangeReason reason, string? triggerId);

    /// <summary>
    /// Called when the handle state has changed.
    /// </summary>
    void OnStateChanged();
}

/// <summary>
/// Factory methods for creating dialog handles.
/// </summary>
public static class DialogHandleFactory
{
    /// <summary>
    /// Creates a new handle to connect a Dialog.Root with detached Dialog.Trigger components.
    /// </summary>
    /// <typeparam name="TPayload">The type of payload to pass to the dialog.</typeparam>
    /// <returns>A new DialogHandle instance.</returns>
    public static DialogHandle<TPayload> CreateHandle<TPayload>()
    {
        return new DialogHandle<TPayload>();
    }

    /// <summary>
    /// Creates a new handle to connect a Dialog.Root with detached Dialog.Trigger components.
    /// </summary>
    /// <returns>A new DialogHandle instance.</returns>
    public static DialogHandle CreateHandle()
    {
        return new DialogHandle();
    }
}
