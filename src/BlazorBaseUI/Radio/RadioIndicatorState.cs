namespace BlazorBaseUI.Radio;

public sealed record RadioIndicatorState(
    bool Checked,
    bool Disabled,
    bool ReadOnly,
    bool Required,
    bool? Valid,
    bool Touched,
    bool Dirty,
    bool Filled,
    bool Focused,
    TransitionStatus TransitionStatus);
