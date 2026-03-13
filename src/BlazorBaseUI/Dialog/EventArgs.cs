namespace BlazorBaseUI.Dialog;

/// <summary>
/// Provides data for the dialog open change event.
/// </summary>
public sealed class DialogOpenChangeEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DialogOpenChangeEventArgs"/> class.
    /// </summary>
    /// <param name="open">The new open state.</param>
    /// <param name="reason">The reason for the state change.</param>
    public DialogOpenChangeEventArgs(bool open, OpenChangeReason reason)
    {
        Open = open;
        Reason = reason;
    }

    /// <summary>
    /// Gets the new open state of the dialog.
    /// </summary>
    public bool Open { get; }

    /// <summary>
    /// Gets the reason the dialog's open state changed.
    /// </summary>
    public OpenChangeReason Reason { get; }

    /// <summary>
    /// Gets a value indicating whether the open change was canceled.
    /// </summary>
    public bool Canceled { get; private set; }

    /// <summary>
    /// Cancels the open state change, preventing the dialog from opening or closing.
    /// </summary>
    public void Cancel() => Canceled = true;

    /// <summary>
    /// Gets a value indicating whether the dialog popup should be prevented from unmounting after the close transition completes.
    /// </summary>
    public bool PreventUnmountingOnClose { get; private set; }

    /// <summary>
    /// Prevents the dialog popup from automatically unmounting after the close transition ends.
    /// The consumer is responsible for calling <see cref="DialogRootActions.Unmount"/> to unmount it manually.
    /// </summary>
    public void PreventUnmountOnClose() => PreventUnmountingOnClose = true;
}
