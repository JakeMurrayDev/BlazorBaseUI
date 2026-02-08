namespace BlazorBaseUI.Collapsible;

/// <summary>
/// Provides data for the <see cref="CollapsibleRoot.OnOpenChange"/> event.
/// </summary>
public class CollapsibleOpenChangeEventArgs : EventArgs
{
    /// <summary>
    /// Gets the new open state of the collapsible.
    /// </summary>
    public bool Open { get; }

    /// <summary>
    /// Gets the reason the open state changed.
    /// </summary>
    public CollapsibleOpenChangeReason Reason { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the open change has been canceled.
    /// </summary>
    public bool Canceled { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="CollapsibleOpenChangeEventArgs"/>.
    /// </summary>
    /// <param name="open">The new open state of the collapsible.</param>
    /// <param name="reason">The reason the open state changed.</param>
    public CollapsibleOpenChangeEventArgs(bool open, CollapsibleOpenChangeReason reason = CollapsibleOpenChangeReason.None)
    {
        Open = open;
        Reason = reason;
    }
}

/// <summary>
/// Describes the reason a collapsible open state change was triggered.
/// </summary>
public enum CollapsibleOpenChangeReason
{
    /// <summary>
    /// No specific reason.
    /// </summary>
    None,

    /// <summary>
    /// The trigger button was pressed.
    /// </summary>
    TriggerPress
}
