namespace BlazorBaseUI.Tabs;

public sealed record TabsIndicatorState(
    Orientation Orientation,
    ActivationDirection ActivationDirection,
    TabPosition? ActiveTabPosition,
    TabSize? ActiveTabSize)
{
    internal static TabsIndicatorState Default { get; } = new(
        Orientation: Orientation.Horizontal,
        ActivationDirection: ActivationDirection.None,
        ActiveTabPosition: null,
        ActiveTabSize: null);
}
