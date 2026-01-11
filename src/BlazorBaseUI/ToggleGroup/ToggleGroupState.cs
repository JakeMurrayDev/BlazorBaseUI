namespace BlazorBaseUI.ToggleGroup;

public sealed record ToggleGroupState(bool Disabled, bool Multiple, Orientation Orientation)
{
    public static ToggleGroupState Default { get; } = new(
        Disabled: false,
        Multiple: false,
        Orientation: Orientation.Horizontal);
}
