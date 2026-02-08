namespace BlazorBaseUI.Accordion;

/// <summary>
/// Represents the state of the <see cref="AccordionPanel"/> component.
/// </summary>
/// <param name="Open">Whether the panel is open.</param>
/// <param name="Disabled">Whether the item is disabled.</param>
/// <param name="Index">The index of the accordion item.</param>
/// <param name="Orientation">The visual orientation of the accordion.</param>
/// <param name="TransitionStatus">The current transition status of the panel.</param>
public sealed record AccordionPanelState(
    bool Open,
    bool Disabled,
    int Index,
    Orientation Orientation,
    TransitionStatus TransitionStatus);
