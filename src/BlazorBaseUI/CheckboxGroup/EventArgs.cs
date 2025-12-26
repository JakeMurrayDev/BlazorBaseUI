namespace BlazorBaseUI.CheckboxGroup;

public class CheckboxGroupValueChangeEventArgs : EventArgs
{
    public string[] Value { get; }
    public bool IsCanceled { get; private set; }

    public CheckboxGroupValueChangeEventArgs(string[] value)
    {
        Value = value;
    }

    public void Cancel() => IsCanceled = true;
}
