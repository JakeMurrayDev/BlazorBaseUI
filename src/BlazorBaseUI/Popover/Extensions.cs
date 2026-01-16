namespace BlazorBaseUI.Popover;

internal static class Extensions
{
    public static string? ToDataAttributeString(this Side side) => side switch
    {
        Side.Top => "top",
        Side.Bottom => "bottom",
        Side.Left => "left",
        Side.Right => "right",
        Side.InlineEnd => "inline-end",
        Side.InlineStart => "inline-start",
        _ => null
    };

    public static string? ToDataAttributeString(this Align align) => align switch
    {
        Align.Start => "start",
        Align.Center => "center",
        Align.End => "end",
        _ => null
    };

    public static string? ToDataAttributeString(this TransitionStatus status) => status switch
    {
        TransitionStatus.Starting => "starting",
        TransitionStatus.Ending => "ending",
        _ => null
    };

    public static string? ToDataAttributeString(this InstantType instant) => instant switch
    {
        InstantType.Click => "click",
        InstantType.Dismiss => "dismiss",
        _ => null
    };
}
