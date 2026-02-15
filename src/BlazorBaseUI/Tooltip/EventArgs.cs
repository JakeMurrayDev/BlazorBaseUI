namespace BlazorBaseUI.Tooltip;

/// <summary>
/// Provides data for the tooltip open state change event.
/// </summary>
public sealed class TooltipOpenChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TooltipOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The requested open state of the tooltip.</param>
    /// <param name="reason">The reason for the open state change.</param>
    public TooltipOpenChangeEventArgs(bool open, TooltipOpenChangeReason reason)
    {
        Open = open;
        Reason = reason;
    }

    /// <summary>
    /// Gets the requested open state of the tooltip.
    /// </summary>
    public bool Open { get; }

    /// <summary>
    /// Gets the reason for the open state change.
    /// </summary>
    public TooltipOpenChangeReason Reason { get; }

    /// <summary>
    /// Gets a value indicating whether the open state change has been canceled.
    /// </summary>
    public bool Canceled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether unmounting on close should be prevented.
    /// </summary>
    public bool PreventUnmount { get; private set; }

    /// <summary>
    /// Cancels the open state change, preventing the tooltip from opening or closing.
    /// </summary>
    public void Cancel() => Canceled = true;

    /// <summary>
    /// Prevents the tooltip from unmounting when it closes.
    /// </summary>
    public void PreventUnmountOnClose() => PreventUnmount = true;
}
