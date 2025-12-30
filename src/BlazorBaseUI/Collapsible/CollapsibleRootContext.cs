namespace BlazorBaseUI.Collapsible;

public record CollapsibleRootContext(
    bool Open,
    bool Disabled,
    string PanelId,
    Action HandleTrigger)
{
    public CollapsibleRootState State => new(Open, Disabled, TransitionStatus.Undefined);
}