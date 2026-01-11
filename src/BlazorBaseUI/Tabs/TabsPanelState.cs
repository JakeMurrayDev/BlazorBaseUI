namespace BlazorBaseUI.Tabs;

public sealed record TabsPanelState(bool Hidden, Orientation Orientation, ActivationDirection ActivationDirection)
{
    public static TabsPanelState Default { get; } = new(
        Hidden: true,
        Orientation: Orientation.Horizontal,
        ActivationDirection: ActivationDirection.None);
}
