namespace BlazorBaseUI.Menu;

public readonly record struct MenuRadioItemIndicatorState(
    bool Checked,
    bool Disabled,
    bool Highlighted,
    TransitionStatus TransitionStatus);
