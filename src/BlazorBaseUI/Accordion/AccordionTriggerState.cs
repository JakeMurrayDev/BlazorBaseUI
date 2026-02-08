namespace BlazorBaseUI.Accordion;

/// <summary>
/// Represents the state of the <see cref="AccordionTrigger"/> component.
/// </summary>
/// <param name="Open">Whether the associated panel is open.</param>
/// <param name="Orientation">The visual orientation of the accordion.</param>
/// <param name="Value">The string value of the accordion item.</param>
/// <param name="Disabled">Whether the trigger is disabled.</param>
public sealed record AccordionTriggerState(
    bool Open,
    Orientation Orientation,
    string Value,
    bool Disabled);
