namespace BlazorBaseUI.Collapsible;

public sealed record CollapsibleRootState(
    bool Open,
    bool Disabled,
    TransitionStatus TransitionStatus);
