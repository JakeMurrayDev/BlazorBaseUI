using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Popover;

/// <summary>
/// Provides cascading state and callbacks from the <see cref="PopoverRoot"/> to child popover components.
/// </summary>
internal sealed class PopoverRootContext
{
    /// <summary>
    /// Gets or sets the unique identifier for the popover root instance.
    /// </summary>
    public string RootId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the popover is currently open.
    /// </summary>
    public bool Open { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the popover is currently mounted in the DOM.
    /// </summary>
    public bool Mounted { get; set; }

    /// <summary>
    /// Gets or sets the modal behavior of the popover.
    /// </summary>
    public ModalMode Modal { get; set; }

    /// <summary>
    /// Gets or sets the reason for the most recent open state change.
    /// </summary>
    public OpenChangeReason OpenChangeReason { get; set; }

    /// <summary>
    /// Gets or sets the current transition status of the popover.
    /// </summary>
    public TransitionStatus TransitionStatus { get; set; }

    /// <summary>
    /// Gets or sets the type of instant transition currently in effect.
    /// </summary>
    public InstantType InstantType { get; set; }

    /// <summary>
    /// Gets or sets the element ID of the popover title, used for <c>aria-labelledby</c>.
    /// </summary>
    public string? TitleId { get; set; }

    /// <summary>
    /// Gets or sets the element ID of the popover description, used for <c>aria-describedby</c>.
    /// </summary>
    public string? DescriptionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the currently active trigger.
    /// </summary>
    public string? ActiveTriggerId { get; set; }

    /// <summary>
    /// Gets or sets the payload associated with the current popover interaction.
    /// </summary>
    public object? Payload { get; set; }

    /// <summary>
    /// Gets or sets a delegate that returns the current open state.
    /// </summary>
    public Func<bool> GetOpen { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that returns the current mounted state.
    /// </summary>
    public Func<bool> GetMounted { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that returns the trigger element reference.
    /// </summary>
    public Func<ElementReference?> GetTriggerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that returns the positioner element reference.
    /// </summary>
    public Func<ElementReference?> GetPositionerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that returns the popup element reference.
    /// </summary>
    public Func<ElementReference?> GetPopupElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the title element ID.
    /// </summary>
    public Action<string> SetTitleId { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the description element ID.
    /// </summary>
    public Action<string> SetDescriptionId { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the trigger element reference.
    /// </summary>
    public Action<ElementReference?> SetTriggerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the positioner element reference.
    /// </summary>
    public Action<ElementReference?> SetPositionerElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that sets the popup element reference.
    /// </summary>
    public Action<ElementReference?> SetPopupElement { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that asynchronously sets the open state with a reason, optional payload, and optional trigger ID.
    /// </summary>
    public Func<bool, OpenChangeReason, object?, string?, Task> SetOpenAsync { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that closes the popover.
    /// </summary>
    public Action Close { get; set; } = null!;

    /// <summary>
    /// Gets or sets a delegate that forces the popover to unmount immediately.
    /// </summary>
    public Action ForceUnmount { get; set; } = null!;
}
