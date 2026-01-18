using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tooltip;

/// <summary>
/// Non-generic interface for TooltipHandle that allows TooltipRoot to interact with handles
/// without knowing the payload type at compile time.
/// </summary>
public interface ITooltipHandle
{
    /// <summary>
    /// Gets a value indicating whether the tooltip is currently open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets the ID of the currently active trigger.
    /// </summary>
    string? ActiveTriggerId { get; }

    /// <summary>
    /// Opens the tooltip and associates it with the trigger with the given ID.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the tooltip.</param>
    void Open(string triggerId);

    /// <summary>
    /// Closes the tooltip.
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
    internal void Subscribe(ITooltipHandleSubscriber subscriber);

    /// <summary>
    /// Unsubscribes a component from handle state changes.
    /// </summary>
    internal void Unsubscribe(ITooltipHandleSubscriber subscriber);

    /// <summary>
    /// Called by root to sync state back to handle after processing.
    /// </summary>
    internal void SyncState(bool open, string? triggerId, object? payload);
}

/// <summary>
/// A handle to control a tooltip imperatively and to associate detached triggers with it.
/// The handle owns the tooltip state and coordinates between detached Root and Trigger components.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the tooltip.</typeparam>
public class TooltipHandle<TPayload> : ITooltipHandle
{
    private readonly Dictionary<string, TriggerData> registeredTriggers = new();
    private readonly List<ITooltipHandleSubscriber> subscribers = new();

    private bool isOpen;
    private string? activeTriggerId;
    private TPayload? payload;

    /// <summary>
    /// Gets a value indicating whether the tooltip is currently open.
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
    /// Opens the tooltip and associates it with the trigger with the given ID.
    /// The trigger must be a TooltipTrigger component with this handle passed as a prop.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the tooltip.</param>
    /// <exception cref="InvalidOperationException">Thrown when no trigger is found with the given ID.</exception>
    public void Open(string triggerId)
    {
        if (string.IsNullOrEmpty(triggerId))
        {
            throw new ArgumentException("Trigger ID cannot be null or empty.", nameof(triggerId));
        }

        if (!registeredTriggers.ContainsKey(triggerId))
        {
            throw new InvalidOperationException($"TooltipHandle.Open: No trigger found with id \"{triggerId}\".");
        }

        SetOpenInternal(true, TooltipOpenChangeReason.ImperativeAction, triggerId);
    }

    /// <summary>
    /// Closes the tooltip.
    /// </summary>
    public void Close()
    {
        SetOpenInternal(false, TooltipOpenChangeReason.ImperativeAction, null);
    }

    /// <summary>
    /// Registers a trigger with this handle.
    /// </summary>
    internal void RegisterTrigger(string triggerId, ElementReference? element, TPayload? triggerPayload)
    {
        registeredTriggers[triggerId] = new TriggerData(element, triggerPayload);

        foreach (var subscriber in subscribers)
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

        foreach (var subscriber in subscribers)
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

            foreach (var subscriber in subscribers)
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

            // If this trigger is active and tooltip is open, update the current payload
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
    internal void Subscribe(ITooltipHandleSubscriber subscriber)
    {
        if (!subscribers.Contains(subscriber))
        {
            subscribers.Add(subscriber);
        }
    }

    /// <summary>
    /// Unsubscribes a component from handle state changes.
    /// </summary>
    internal void Unsubscribe(ITooltipHandleSubscriber subscriber)
    {
        subscribers.Remove(subscriber);
    }

    /// <summary>
    /// Called by triggers to request opening the tooltip.
    /// </summary>
    internal void RequestOpen(string triggerId, TooltipOpenChangeReason reason)
    {
        SetOpenInternal(true, reason, triggerId);
    }

    /// <summary>
    /// Called by triggers to request closing the tooltip.
    /// </summary>
    internal void RequestClose(TooltipOpenChangeReason reason)
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
    ElementReference? ITooltipHandle.GetTriggerElement(string? triggerId)
    {
        return GetTriggerElement(triggerId);
    }

    /// <inheritdoc />
    object? ITooltipHandle.GetTriggerPayloadAsObject(string? triggerId)
    {
        return GetTriggerPayload(triggerId);
    }

    /// <inheritdoc />
    void ITooltipHandle.Subscribe(ITooltipHandleSubscriber subscriber)
    {
        Subscribe(subscriber);
    }

    /// <inheritdoc />
    void ITooltipHandle.Unsubscribe(ITooltipHandleSubscriber subscriber)
    {
        Unsubscribe(subscriber);
    }

    /// <inheritdoc />
    void ITooltipHandle.SyncState(bool open, string? triggerId, object? payload)
    {
        SyncState(open, triggerId, payload is TPayload typedPayload ? typedPayload : default);
    }

    private void SetOpenInternal(bool nextOpen, TooltipOpenChangeReason reason, string? triggerId)
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
        foreach (var subscriber in subscribers)
        {
            subscriber.OnOpenChangeRequested(nextOpen, reason, triggerId);
        }
    }

    private void NotifyStateChanged()
    {
        foreach (var subscriber in subscribers)
        {
            subscriber.OnStateChanged();
        }
    }

    private readonly record struct TriggerData(ElementReference? Element, TPayload? Payload);
}

/// <summary>
/// Non-generic version of TooltipHandle for scenarios where payload type is not needed.
/// </summary>
public sealed class TooltipHandle : TooltipHandle<object?>
{
}

/// <summary>
/// Interface for components that subscribe to TooltipHandle state changes.
/// </summary>
internal interface ITooltipHandleSubscriber
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
    void OnOpenChangeRequested(bool open, TooltipOpenChangeReason reason, string? triggerId);

    /// <summary>
    /// Called when the handle state has changed.
    /// </summary>
    void OnStateChanged();
}

/// <summary>
/// Factory methods for creating tooltip handles.
/// </summary>
public static class TooltipHandleFactory
{
    /// <summary>
    /// Creates a new handle to connect a Tooltip.Root with detached Tooltip.Trigger components.
    /// </summary>
    /// <typeparam name="TPayload">The type of payload to pass to the tooltip.</typeparam>
    /// <returns>A new TooltipHandle instance.</returns>
    public static TooltipHandle<TPayload> CreateHandle<TPayload>()
    {
        return new TooltipHandle<TPayload>();
    }

    /// <summary>
    /// Creates a new handle to connect a Tooltip.Root with detached Tooltip.Trigger components.
    /// </summary>
    /// <returns>A new TooltipHandle instance.</returns>
    public static TooltipHandle CreateHandle()
    {
        return new TooltipHandle();
    }
}
