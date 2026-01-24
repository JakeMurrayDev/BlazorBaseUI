namespace BlazorBaseUI.Slider;

public class SliderValueChangeEventArgs<TValue> : EventArgs where TValue : notnull
{
    public TValue Value { get; }
    public SliderChangeReason Reason { get; }
    public int ActiveThumbIndex { get; }
    public bool IsCanceled { get; private set; }

    public SliderValueChangeEventArgs(TValue value, SliderChangeReason reason, int activeThumbIndex)
    {
        Value = value;
        Reason = reason;
        ActiveThumbIndex = activeThumbIndex;
    }

    public void Cancel() => IsCanceled = true;
}

public class SliderValueCommittedEventArgs<TValue> : EventArgs where TValue : notnull
{
    public TValue Value { get; }
    public SliderChangeReason Reason { get; }

    public SliderValueCommittedEventArgs(TValue value, SliderChangeReason reason)
    {
        Value = value;
        Reason = reason;
    }
}
