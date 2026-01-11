namespace BlazorBaseUI.Tabs;

public sealed record TabsTabState(bool Active, bool Disabled, Orientation Orientation)
{
    internal static TabsTabState Default { get; } = new(
        Active: false,
        Disabled: false,
        Orientation: Orientation.Horizontal);
}
