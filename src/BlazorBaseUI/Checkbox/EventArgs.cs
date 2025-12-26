namespace BlazorBaseUI.Checkbox;

public class CheckboxCheckedChangeEventArgs : EventArgs
{
    public bool Checked { get; }
    public bool IsCanceled { get; private set; }

    public CheckboxCheckedChangeEventArgs(bool isChecked)
    {
        Checked = isChecked;
    }

    public void Cancel() => IsCanceled = true;
}