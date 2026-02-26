using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.PreviewCard;

/// <summary>
/// Non-generic interface for PreviewCardHandle that allows PreviewCardRoot to interact with handles
/// without knowing the payload type at compile time.
/// </summary>
public interface IPreviewCardHandle
{
    /// <summary>
    /// Gets a value indicating whether the preview card is currently open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets the ID of the currently active trigger.
    /// </summary>
    string? ActiveTriggerId { get; }

    /// <summary>
    /// Opens the preview card and associates it with the trigger with the given ID.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the preview card.</param>
    void Open(string triggerId);

    /// <summary>
    /// Closes the preview card.
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
    internal void Subscribe(IPreviewCardHandleSubscriber subscriber);

    /// <summary>
    /// Unsubscribes a component from handle state changes.
    /// </summary>
    internal void Unsubscribe(IPreviewCardHandleSubscriber subscriber);

    /// <summary>
    /// Called by root to sync state back to handle after processing.
    /// </summary>
    internal void SyncState(bool open, string? triggerId, object? payload);
}

/// <summary>
/// A handle to control a preview card imperatively and to associate detached triggers with it.
/// The handle owns the preview card state and coordinates between detached Root and Trigger components.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the preview card.</typeparam>
public class PreviewCardHandle<TPayload> : IPreviewCardHandle
{
    private readonly Dictionary<string, TriggerData> registeredTriggers = new();
    private readonly List<IPreviewCardHandleSubscriber> subscribers = new();

    private bool isOpen;
    private string? activeTriggerId;
    private TPayload? payload;

    /// <summary>
    /// Gets a value indicating whether the preview card is currently open.
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
    /// Opens the preview card and associates it with the trigger with the given ID.
    /// The trigger must be a PreviewCardTrigger component with this handle passed as a prop.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the preview card.</param>
    /// <exception cref="InvalidOperationException">Thrown when no trigger is found with the given ID.</exception>
    public void Open(string triggerId)
    {
        if (string.IsNullOrEmpty(triggerId))
        {
            throw new ArgumentException("Trigger ID cannot be null or empty.", nameof(triggerId));
        }

        if (!registeredTriggers.ContainsKey(triggerId))
        {
            throw new InvalidOperationException($"PreviewCardHandle.Open: No trigger found with id \"{triggerId}\".");
        }

        SetOpenInternal(true, PreviewCardOpenChangeReason.ImperativeAction, triggerId);
    }

    /// <summary>
    /// Closes the preview card.
    /// </summary>
    public void Close()
    {
        SetOpenInternal(false, PreviewCardOpenChangeReason.ImperativeAction, null);
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

            // If this trigger is active and preview card is open, update the current payload
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
    /// Subscribes a component to handle state changes.
    /// </summary>
    internal void Subscribe(IPreviewCardHandleSubscriber subscriber)
    {
        if (!subscribers.Contains(subscriber))
        {
            subscribers.Add(subscriber);
        }
    }

    /// <summary>
    /// Unsubscribes a component from handle state changes.
    /// </summary>
    internal void Unsubscribe(IPreviewCardHandleSubscriber subscriber)
    {
        subscribers.Remove(subscriber);
    }

    /// <summary>
    /// Called by triggers to request opening the preview card.
    /// </summary>
    internal void RequestOpen(string triggerId, PreviewCardOpenChangeReason reason)
    {
        SetOpenInternal(true, reason, triggerId);
    }

    /// <summary>
    /// Called by triggers to request closing the preview card.
    /// </summary>
    internal void RequestClose(PreviewCardOpenChangeReason reason)
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
    ElementReference? IPreviewCardHandle.GetTriggerElement(string? triggerId)
    {
        return GetTriggerElement(triggerId);
    }

    /// <inheritdoc />
    object? IPreviewCardHandle.GetTriggerPayloadAsObject(string? triggerId)
    {
        return GetTriggerPayload(triggerId);
    }

    /// <inheritdoc />
    void IPreviewCardHandle.Subscribe(IPreviewCardHandleSubscriber subscriber)
    {
        Subscribe(subscriber);
    }

    /// <inheritdoc />
    void IPreviewCardHandle.Unsubscribe(IPreviewCardHandleSubscriber subscriber)
    {
        Unsubscribe(subscriber);
    }

    /// <inheritdoc />
    void IPreviewCardHandle.SyncState(bool open, string? triggerId, object? payload)
    {
        SyncState(open, triggerId, payload is TPayload typedPayload ? typedPayload : default);
    }

    private void SetOpenInternal(bool nextOpen, PreviewCardOpenChangeReason reason, string? triggerId)
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

        // Notify all subscribers (the root will actually process the state change)
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
/// Non-generic version of PreviewCardHandle for scenarios where payload type is not needed.
/// </summary>
public sealed class PreviewCardHandle : PreviewCardHandle<object?>
{
}

/// <summary>
/// Interface for components that subscribe to PreviewCardHandle state changes.
/// </summary>
internal interface IPreviewCardHandleSubscriber
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
    void OnOpenChangeRequested(bool open, PreviewCardOpenChangeReason reason, string? triggerId);

    /// <summary>
    /// Called when the handle state has changed.
    /// </summary>
    void OnStateChanged();
}

/// <summary>
/// Factory methods for creating preview card handles.
/// </summary>
public static class PreviewCardHandleFactory
{
    /// <summary>
    /// Creates a new handle to connect a PreviewCard.Root with detached PreviewCard.Trigger components.
    /// </summary>
    /// <typeparam name="TPayload">The type of payload to pass to the preview card.</typeparam>
    /// <returns>A new PreviewCardHandle instance.</returns>
    public static PreviewCardHandle<TPayload> CreateHandle<TPayload>()
    {
        return new PreviewCardHandle<TPayload>();
    }

    /// <summary>
    /// Creates a new handle to connect a PreviewCard.Root with detached PreviewCard.Trigger components.
    /// </summary>
    /// <returns>A new PreviewCardHandle instance.</returns>
    public static PreviewCardHandle CreateHandle()
    {
        return new PreviewCardHandle();
    }
}
