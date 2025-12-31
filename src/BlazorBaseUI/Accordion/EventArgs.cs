namespace BlazorBaseUI.Accordion;

public sealed class AccordionValueChangeEventArgs<TValue> : EventArgs
{
    public AccordionValueChangeEventArgs(TValue[] value)
    {
        Value = value;
    }

    public TValue[] Value { get; }

    public bool Canceled { get; set; }
}
