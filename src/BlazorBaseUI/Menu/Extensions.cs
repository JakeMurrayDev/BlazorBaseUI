namespace BlazorBaseUI.Menu;

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

    public static string? ToDataAttributeString(this InstantType instant) => instant switch
    {
        InstantType.Click => "click",
        InstantType.Dismiss => "dismiss",
        InstantType.Group => "group",
        _ => null
    };

    public static string ToDataAttributeString(this CollisionBoundary collisionBoundary) => collisionBoundary switch
    {
        CollisionBoundary.Viewport => "viewport",
        CollisionBoundary.ClippingAncestors => "clipping-ancestors",
        _ => "clipping-ancestors"
    };

    public static string ToDataAttributeString(this CollisionAvoidance collisionAvoidance) => collisionAvoidance switch
    {
        CollisionAvoidance.None => "none",
        CollisionAvoidance.Shift => "shift",
        CollisionAvoidance.Flip => "flip",
        CollisionAvoidance.FlipShift => "flip-shift",
        _ => "flip-shift"
    };
}
