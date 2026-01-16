namespace BlazorBaseUI.Popover;

public sealed class PopoverOpenChangeEventArgs : EventArgs
{
    public PopoverOpenChangeEventArgs(bool open, OpenChangeReason reason)
    {
        Open = open;
        Reason = reason;
    }

    public bool Open { get; }

    public OpenChangeReason Reason { get; }

    public bool Canceled { get; private set; }

    public void Cancel() => Canceled = true;
}
