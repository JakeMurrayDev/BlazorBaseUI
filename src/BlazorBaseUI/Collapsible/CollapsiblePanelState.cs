namespace BlazorBaseUI.Collapsible;

public sealed record CollapsiblePanelState(
    bool Open,
    bool Disabled,
    TransitionStatus TransitionStatus);
