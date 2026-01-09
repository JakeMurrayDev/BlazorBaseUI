namespace BlazorBaseUI.Accordion;

public sealed record AccordionTriggerState(
    bool Open,
    Orientation Orientation,
    string Value,
    bool Disabled);
