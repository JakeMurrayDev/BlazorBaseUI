namespace BlazorBaseUI.RadioGroup;

public class RadioGroupValueChangeEventArgs<TValue> : EventArgs
{
    public TValue? Value { get; }
    public bool IsCanceled { get; private set; }

    public RadioGroupValueChangeEventArgs(TValue? value)
    {
        Value = value;
    }

    public void Cancel() => IsCanceled = true;
}
