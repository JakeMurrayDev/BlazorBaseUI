namespace BlazorBaseUI.Dialog;

public sealed class DialogOpenChangeEventArgs : EventArgs
{
    public DialogOpenChangeEventArgs(bool open, OpenChangeReason reason)
    {
        Open = open;
        Reason = reason;
    }

    public bool Open { get; }

    public OpenChangeReason Reason { get; }

    public bool Canceled { get; private set; }

    public void Cancel() => Canceled = true;
}
