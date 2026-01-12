namespace BlazorBaseUI.NumberField;

public sealed class NumberFieldValueChangeEventArgs : EventArgs
{
    public NumberFieldValueChangeEventArgs(
        double? value,
        NumberFieldChangeReason reason,
        int? direction = null)
    {
        Value = value;
        Reason = reason;
        Direction = direction;
    }

    public double? Value { get; }

    public NumberFieldChangeReason Reason { get; }

    public int? Direction { get; }

    public bool IsCanceled { get; private set; }

    public void Cancel() => IsCanceled = true;
}

public sealed class NumberFieldValueCommittedEventArgs : EventArgs
{
    public NumberFieldValueCommittedEventArgs(
        double? value,
        NumberFieldChangeReason reason)
    {
        Value = value;
        Reason = reason;
    }

    public double? Value { get; }

    public NumberFieldChangeReason Reason { get; }
}
