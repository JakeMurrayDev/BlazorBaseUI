namespace BlazorBaseUI.Accordion;

public sealed record AccordionHeaderState(
    int Index,
    Orientation Orientation,
    bool Disabled,
    bool Open);
