namespace BlazorBaseUI.Toggle;

public sealed record ToggleState(bool Pressed, bool Disabled)
{
    public static ToggleState Default { get; } = new(Pressed: false, Disabled: false);
}
