namespace BlazorBaseUI.Accordion;

public sealed record AccordionItemState<TValue>(
    TValue[] Value,
    bool Disabled,
    Orientation Orientation,
    int Index,
    bool Open);
