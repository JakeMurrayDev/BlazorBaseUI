namespace BlazorBaseUI.Tooltip;

public sealed class TooltipOpenChangeEventArgs : EventArgs
{
    public TooltipOpenChangeEventArgs(bool open, TooltipOpenChangeReason reason)
    {
        Open = open;
        Reason = reason;
    }

    public bool Open { get; }

    public TooltipOpenChangeReason Reason { get; }

    public bool Canceled { get; private set; }

    public bool PreventUnmount { get; private set; }

    public void Cancel() => Canceled = true;

    public void PreventUnmountOnClose() => PreventUnmount = true;
}
