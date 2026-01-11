namespace BlazorBaseUI.Accordion;

public sealed record AccordionPanelState(
    bool Open,
    bool Disabled,
    int Index,
    Orientation Orientation,
    TransitionStatus TransitionStatus);
