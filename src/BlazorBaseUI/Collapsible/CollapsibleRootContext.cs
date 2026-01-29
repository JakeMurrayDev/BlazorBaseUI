namespace BlazorBaseUI.Collapsible;

public sealed record CollapsibleRootContext(
    bool Open,
    bool Disabled,
    TransitionStatus TransitionStatus,
    string PanelId,
    Action HandleTrigger,
    Action HandleBeforeMatch,
    Action<string> SetPanelId,
    Action<TransitionStatus> SetTransitionStatus);
