using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Menu;

/// <summary>
/// Non-generic interface for MenuHandle that allows MenuRoot to interact with handles
/// without knowing the payload type at compile time.
/// </summary>
public interface IMenuHandle
{
    /// <summary>
    /// Gets a value indicating whether the menu is currently open.
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets the ID of the currently active trigger.
    /// </summary>
    string? ActiveTriggerId { get; }

    /// <summary>
    /// Gets the unique identifier for the popup element.
    /// </summary>
    string? PopupId { get; }

    /// <summary>
    /// Opens the menu and associates it with the trigger with the given ID.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the menu.</param>
    void Open(string triggerId);

    /// <summary>
    /// Closes the menu.
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
    internal void Subscribe(IMenuHandleSubscriber subscriber);

    /// <summary>
    /// Unsubscribes a component from handle state changes.
    /// </summary>
    internal void Unsubscribe(IMenuHandleSubscriber subscriber);

    /// <summary>
    /// Called by root to sync state back to handle after processing.
    /// </summary>
    internal void SyncState(bool open, string? triggerId, object? payload);

    /// <summary>
    /// Sets the unique identifier for the popup element.
    /// </summary>
    internal void SetPopupId(string? popupId);
}

/// <summary>
/// A handle to control a menu imperatively and to associate detached triggers with it.
/// The handle owns the menu state and coordinates between detached Root and Trigger components.
/// </summary>
/// <typeparam name="TPayload">The type of payload to pass to the menu.</typeparam>
public class MenuHandle<TPayload> : IMenuHandle
{
    private readonly Dictionary<string, TriggerData> registeredTriggers = [];
    private readonly List<IMenuHandleSubscriber> subscribers = [];

    private bool isOpen;
    private string? activeTriggerId;
    private string? popupId;
    private TPayload? payload;

    /// <summary>
    /// Gets a value indicating whether the menu is currently open.
    /// </summary>
    public bool IsOpen => isOpen;

    /// <summary>
    /// Gets the ID of the currently active trigger.
    /// </summary>
    public string? ActiveTriggerId => activeTriggerId;

    /// <summary>
    /// Gets the unique identifier for the popup element.
    /// </summary>
    public string? PopupId => popupId;

    /// <summary>
    /// Gets the current payload value.
    /// </summary>
    public TPayload? Payload => payload;

    /// <summary>
    /// Opens the menu and associates it with the trigger with the given ID.
    /// The trigger must be a MenuTypedTrigger component with this handle passed as a parameter.
    /// </summary>
    /// <param name="triggerId">ID of the trigger to associate with the menu.</param>
    /// <exception cref="InvalidOperationException">Thrown when no trigger is found with the given ID.</exception>
    public void Open(string triggerId)
    {
        if (string.IsNullOrEmpty(triggerId))
        {
            throw new ArgumentException("Trigger ID cannot be null or empty.", nameof(triggerId));
        }

        if (!registeredTriggers.ContainsKey(triggerId))
        {
            throw new InvalidOperationException($"MenuHandle.Open: No trigger found with id \"{triggerId}\".");
        }

        SetOpenInternal(true, MenuOpenChangeReason.ImperativeAction, triggerId);
    }

    /// <summary>
    /// Closes the menu.
    /// </summary>
    public void Close()
    {
        SetOpenInternal(false, MenuOpenChangeReason.ImperativeAction, null);
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
    /// Subscribes a component to handle state changes.
    /// </summary>
    internal void Subscribe(IMenuHandleSubscriber subscriber)
    {
        if (!subscribers.Contains(subscriber))
        {
            subscribers.Add(subscriber);
        }
    }

    /// <summary>
    /// Unsubscribes a component from handle state changes.
    /// </summary>
    internal void Unsubscribe(IMenuHandleSubscriber subscriber)
    {
        subscribers.Remove(subscriber);
    }

    /// <summary>
    /// Called by triggers to request opening the menu.
    /// </summary>
    internal void RequestOpen(string triggerId, MenuOpenChangeReason reason)
    {
        SetOpenInternal(true, reason, triggerId);
    }

    /// <summary>
    /// Called by triggers to request closing the menu.
    /// </summary>
    internal void RequestClose(MenuOpenChangeReason reason)
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
    ElementReference? IMenuHandle.GetTriggerElement(string? triggerId)
    {
        return GetTriggerElement(triggerId);
    }

    /// <inheritdoc />
    object? IMenuHandle.GetTriggerPayloadAsObject(string? triggerId)
    {
        return GetTriggerPayload(triggerId);
    }

    /// <inheritdoc />
    void IMenuHandle.Subscribe(IMenuHandleSubscriber subscriber)
    {
        Subscribe(subscriber);
    }

    /// <inheritdoc />
    void IMenuHandle.Unsubscribe(IMenuHandleSubscriber subscriber)
    {
        Unsubscribe(subscriber);
    }

    /// <inheritdoc />
    void IMenuHandle.SyncState(bool open, string? triggerId, object? payload)
    {
        SyncState(open, triggerId, payload is TPayload typedPayload ? typedPayload : default);
    }

    /// <inheritdoc />
    void IMenuHandle.SetPopupId(string? value)
    {
        popupId = value;
    }

    private void SetOpenInternal(bool nextOpen, MenuOpenChangeReason reason, string? triggerId)
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
/// Non-generic version of MenuHandle for scenarios where payload type is not needed.
/// </summary>
public sealed class MenuHandle : MenuHandle<object?>;

/// <summary>
/// Interface for components that subscribe to MenuHandle state changes.
/// </summary>
internal interface IMenuHandleSubscriber
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
    void OnOpenChangeRequested(bool open, MenuOpenChangeReason reason, string? triggerId);

    /// <summary>
    /// Called when the handle state has changed.
    /// </summary>
    void OnStateChanged();
}

/// <summary>
/// Factory methods for creating menu handles.
/// </summary>
public static class MenuHandleFactory
{
    /// <summary>
    /// Creates a new handle to connect a Menu.Root with detached Menu.Trigger components.
    /// </summary>
    /// <typeparam name="TPayload">The type of payload to pass to the menu.</typeparam>
    /// <returns>A new MenuHandle instance.</returns>
    public static MenuHandle<TPayload> CreateHandle<TPayload>()
    {
        return new MenuHandle<TPayload>();
    }

    /// <summary>
    /// Creates a new handle to connect a Menu.Root with detached Menu.Trigger components.
    /// </summary>
    /// <returns>A new MenuHandle instance.</returns>
    public static MenuHandle CreateHandle()
    {
        return new MenuHandle();
    }
}
