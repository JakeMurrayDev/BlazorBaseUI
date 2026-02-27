namespace BlazorBaseUI.Select;

/// <summary>
/// Represents the state of a <see cref="SelectPopup"/> component.
/// </summary>
/// <param name="Open">Whether the select popup is open.</param>
/// <param name="Side">Which side the popup is positioned relative to the trigger.</param>
/// <param name="Align">How the popup is aligned relative to the specified side.</param>
/// <param name="Instant">The type of instant transition to apply.</param>
/// <param name="TransitionStatus">The current transition animation status.</param>
public readonly record struct SelectPopupState(
    bool Open,
    Side Side,
    Align Align,
    InstantType Instant,
    TransitionStatus TransitionStatus);
