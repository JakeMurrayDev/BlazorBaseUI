namespace BlazorBaseUI.Accordion;

public interface IAccordionRootContext
{
    bool Disabled { get; }
    Orientation Orientation { get; }
    Direction Direction { get; }
    bool LoopFocus { get; }
    bool HiddenUntilFound { get; }
    bool KeepMounted { get; }
    bool IsValueOpen(object value);
    void HandleValueChange(object value, bool nextOpen);
}

public sealed record AccordionRootContext<TValue>(
    TValue[] Value,
    bool Disabled,
    Orientation Orientation,
    Direction Direction,
    bool LoopFocus,
    bool HiddenUntilFound,
    bool KeepMounted,
    Action<TValue, bool> OnValueChange) : IAccordionRootContext
{
    public AccordionRootState<TValue> State => new(Value, Disabled, Orientation);

    public bool IsValueOpen(object value)
    {
        if (value is TValue typedValue)
            return Value.Contains(typedValue);
        return false;
    }

    public void HandleValueChange(object value, bool nextOpen)
    {
        if (value is TValue typedValue)
            OnValueChange(typedValue, nextOpen);
    }
}