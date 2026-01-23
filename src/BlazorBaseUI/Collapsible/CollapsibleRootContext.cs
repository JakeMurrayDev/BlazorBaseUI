namespace BlazorBaseUI.Collapsible;

public sealed record CollapsibleRootContext(
    bool Open,
    bool Disabled,
    TransitionStatus TransitionStatus,
    string PanelId,
    Action HandleTrigger,
    Action<string> SetPanelId,
    Action<TransitionStatus> SetTransitionStatus);
