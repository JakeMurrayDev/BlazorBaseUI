namespace BlazorBaseUI.Menu;

public readonly record struct MenuCheckboxItemIndicatorState(
    bool Checked,
    bool Disabled,
    bool Highlighted,
    TransitionStatus TransitionStatus);
