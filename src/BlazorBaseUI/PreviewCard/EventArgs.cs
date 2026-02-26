namespace BlazorBaseUI.PreviewCard;

/// <summary>
/// Provides data for the preview card open state change event.
/// </summary>
public sealed class PreviewCardOpenChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreviewCardOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The requested open state of the preview card.</param>
    /// <param name="reason">The reason for the open state change.</param>
    public PreviewCardOpenChangeEventArgs(bool open, PreviewCardOpenChangeReason reason)
    {
        Open = open;
        Reason = reason;
    }

    /// <summary>
    /// Gets the requested open state of the preview card.
    /// </summary>
    public bool Open { get; }

    /// <summary>
    /// Gets the reason for the open state change.
    /// </summary>
    public PreviewCardOpenChangeReason Reason { get; }

    /// <summary>
    /// Gets a value indicating whether the open state change has been canceled.
    /// </summary>
    public bool Canceled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether unmounting on close should be prevented.
    /// </summary>
    public bool PreventUnmount { get; private set; }

    /// <summary>
    /// Cancels the open state change, preventing the preview card from opening or closing.
    /// </summary>
    public void Cancel() => Canceled = true;

    /// <summary>
    /// Prevents the preview card from unmounting when it closes.
    /// </summary>
    public void PreventUnmountOnClose() => PreventUnmount = true;
}
