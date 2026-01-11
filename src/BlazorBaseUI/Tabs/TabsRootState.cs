namespace BlazorBaseUI.Tabs;

public sealed record TabsRootState(Orientation Orientation, ActivationDirection ActivationDirection)
{
    internal static TabsRootState Default { get; } = new(
        Orientation: Orientation.Horizontal,
        ActivationDirection: ActivationDirection.None);
}
