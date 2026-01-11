namespace BlazorBaseUI.Tabs;

public sealed record TabsPanelState(bool Hidden, Orientation Orientation, ActivationDirection ActivationDirection)
{
    internal static TabsPanelState Default { get; } = new(
        Hidden: true,
        Orientation: Orientation.Horizontal,
        ActivationDirection: ActivationDirection.None);
}
