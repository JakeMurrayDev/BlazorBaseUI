namespace BlazorBaseUI.Radio;

public class RadioCheckedChangeEventArgs<TValue> : EventArgs
{
    public TValue? Value { get; }
    public bool IsCanceled { get; private set; }

    public RadioCheckedChangeEventArgs(TValue? value)
    {
        Value = value;
    }

    public void Cancel() => IsCanceled = true;
}
