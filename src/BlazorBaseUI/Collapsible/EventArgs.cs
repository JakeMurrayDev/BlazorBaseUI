namespace BlazorBaseUI.Collapsible;

public class CollapsibleOpenChangeEventArgs : EventArgs
{
    public CollapsibleOpenChangeEventArgs(bool open, CollapsibleOpenChangeReason reason = CollapsibleOpenChangeReason.None)
    {
        Open = open;
        Reason = reason;
    }

    public bool Open { get; }

    public CollapsibleOpenChangeReason Reason { get; }

    public bool Canceled { get; set; }
}

public enum CollapsibleOpenChangeReason
{
    None,
    TriggerPress
}
