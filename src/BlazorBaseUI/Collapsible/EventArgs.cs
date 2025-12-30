namespace BlazorBaseUI.Collapsible;

public class CollapsibleOpenChangeEventArgs : EventArgs
{
    public CollapsibleOpenChangeEventArgs(bool open)
    {
        Open = open;
    }

    public bool Open { get; }

    public bool Canceled { get; set; }
}