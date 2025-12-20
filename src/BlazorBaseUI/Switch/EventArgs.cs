namespace BlazorBaseUI.Switch;

public class SwitchCheckedChangeEventArgs : EventArgs
{
    public bool Checked { get; }
    public bool IsCanceled { get; private set; }

    public SwitchCheckedChangeEventArgs(bool isChecked)
    {
        Checked = isChecked;
    }

    public void Cancel() => IsCanceled = true;
}
