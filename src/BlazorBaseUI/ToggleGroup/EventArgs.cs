namespace BlazorBaseUI.ToggleGroup;

public class ToggleGroupValueChangeEventArgs : EventArgs
{
    public IReadOnlyList<string> Value { get; }
    public bool IsCanceled { get; private set; }

    public ToggleGroupValueChangeEventArgs(IReadOnlyList<string> value)
    {
        Value = value;
    }

    public void Cancel() => IsCanceled = true;
}
