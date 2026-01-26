namespace BlazorBaseUI.Menu;

public sealed class MenuOpenChangeEventArgs : EventArgs
{
    public MenuOpenChangeEventArgs(bool open, OpenChangeReason reason, object? payload = null)
    {
        Open = open;
        Reason = reason;
        Payload = payload;
    }

    public bool Open { get; }

    public OpenChangeReason Reason { get; }

    public object? Payload { get; }

    public bool Canceled { get; private set; }

    public void Cancel() => Canceled = true;
}

public sealed class MenuRadioGroupChangeEventArgs : EventArgs
{
    public MenuRadioGroupChangeEventArgs(object? value)
    {
        Value = value;
    }

    public object? Value { get; }

    public bool IsCanceled { get; private set; }

    public void Cancel()
    {
        IsCanceled = true;
    }
}

public sealed class MenuCheckboxItemChangeEventArgs : EventArgs
{
    public MenuCheckboxItemChangeEventArgs(bool @checked)
    {
        Checked = @checked;
    }

    public bool Checked { get; }

    public bool IsCanceled { get; private set; }

    public void Cancel()
    {
        IsCanceled = true;
    }
}
