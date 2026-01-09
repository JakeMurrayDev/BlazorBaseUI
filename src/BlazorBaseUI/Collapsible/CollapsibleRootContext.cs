namespace BlazorBaseUI.Collapsible;

public sealed record CollapsibleRootContext(
    bool Open,
    bool Disabled,
    string PanelId,
    Action HandleTrigger,
    Action<string> SetPanelId);
