namespace BlazorBaseUI.Accordion;

public sealed record AccordionRootState<TValue>(
    TValue[] Value,
    bool Disabled,
    Orientation Orientation);
