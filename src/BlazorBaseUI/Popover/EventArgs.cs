namespace BlazorBaseUI.Popover;

/// <summary>
/// Provides data for the popover open state change event.
/// </summary>
public sealed class PopoverOpenChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PopoverOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The requested open state of the popover.</param>
    /// <param name="reason">The reason for the open state change.</param>
    public PopoverOpenChangeEventArgs(bool open, OpenChangeReason reason)
    {
        Open = open;
        Reason = reason;
    }

    /// <summary>
    /// Gets the requested open state of the popover.
    /// </summary>
    public bool Open { get; }

    /// <summary>
    /// Gets the reason for the open state change.
    /// </summary>
    public OpenChangeReason Reason { get; }

    /// <summary>
    /// Gets a value indicating whether the open state change has been canceled.
    /// </summary>
    public bool Canceled { get; private set; }

    /// <summary>
    /// Cancels the open state change, preventing the popover from opening or closing.
    /// </summary>
    public void Cancel() => Canceled = true;
}
