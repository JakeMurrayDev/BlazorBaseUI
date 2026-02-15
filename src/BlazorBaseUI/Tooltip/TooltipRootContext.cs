using BlazorBaseUI.Popover;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tooltip;

/// <summary>
/// Provides shared state and operations for child components of a <see cref="TooltipRoot"/>.
/// </summary>
internal sealed class TooltipRootContext
{
    /// <summary>
    /// Gets or sets the unique identifier of the tooltip root.
    /// </summary>
    public string RootId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the popup element.
    /// </summary>
    public string PopupId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the tooltip is currently open.
    /// </summary>
    public bool Open { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tooltip is mounted in the DOM.
    /// </summary>
    public bool Mounted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the tooltip is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the reason for the most recent open state change.
    /// </summary>
    public TooltipOpenChangeReason OpenChangeReason { get; set; }

    /// <summary>
    /// Gets or sets the current transition status.
    /// </summary>
    public Popover.TransitionStatus TransitionStatus { get; set; }

    /// <summary>
    /// Gets or sets the current instant transition type.
    /// </summary>
    public TooltipInstantType InstantType { get; set; }

    /// <summary>
    /// Gets or sets which axis the tooltip tracks the cursor on.
    /// </summary>
    public TrackCursorAxis TrackCursorAxis { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether hovering over the popup is disabled.
    /// </summary>
    public bool DisableHoverablePopup { get; set; }

    /// <summary>
    /// Gets or sets the ID of the currently active trigger.
    /// </summary>
    public string? ActiveTriggerId { get; set; }

    /// <summary>
    /// Gets or sets the current payload value from the active trigger.
    /// </summary>
    public object? Payload { get; set; }

    /// <summary>
    /// Gets or sets the delegate that returns the current open state.
    /// </summary>
    public Func<bool> GetOpen { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that returns the current mounted state.
    /// </summary>
    public Func<bool> GetMounted { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that returns the current payload.
    /// </summary>
    public Func<object?> GetPayload { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that returns the active trigger element reference.
    /// </summary>
    public Func<ElementReference?> GetTriggerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that registers a trigger element.
    /// </summary>
    public Action<string, ElementReference?> RegisterTriggerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that unregisters a trigger element.
    /// </summary>
    public Action<string> UnregisterTriggerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that stores the positioner element reference.
    /// </summary>
    public Action<ElementReference?> SetPositionerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that stores the popup element reference.
    /// </summary>
    public Action<ElementReference?> SetPopupElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that sets the tooltip's open state.
    /// </summary>
    public Func<bool, TooltipOpenChangeReason, string?, Task> SetOpenAsync { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that sets a trigger's payload.
    /// </summary>
    public Action<string, object?> SetTriggerPayload { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delegate that forces the tooltip to unmount immediately.
    /// </summary>
    public Action ForceUnmount { get; set; } = null!;
}
