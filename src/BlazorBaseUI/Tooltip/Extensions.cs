namespace BlazorBaseUI.Tooltip;

internal static class Extensions
{
    public static string ToDataAttributeString(this TrackCursorAxis axis) => axis switch
    {
        TrackCursorAxis.None => "none",
        TrackCursorAxis.X => "x",
        TrackCursorAxis.Y => "y",
        TrackCursorAxis.Both => "both",
        _ => "none"
    };

    public static string? ToDataAttributeString(this TooltipInstantType instantType) => instantType switch
    {
        TooltipInstantType.Delay => "delay",
        TooltipInstantType.Focus => "focus",
        TooltipInstantType.Dismiss => "dismiss",
        _ => null
    };
}
