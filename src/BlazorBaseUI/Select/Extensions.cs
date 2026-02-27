namespace BlazorBaseUI.Select;

/// <summary>
/// Provides extension methods for converting select enumeration values to their data attribute string representations.
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Converts a <see cref="Side"/> value to its corresponding data attribute string.
    /// </summary>
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

    /// <summary>
    /// Converts an <see cref="Align"/> value to its corresponding data attribute string.
    /// </summary>
    public static string? ToDataAttributeString(this Align align) => align switch
    {
        Align.Start => "start",
        Align.Center => "center",
        Align.End => "end",
        _ => null
    };

    /// <summary>
    /// Converts an <see cref="InstantType"/> value to its corresponding data attribute string.
    /// </summary>
    public static string? ToDataAttributeString(this InstantType instant) => instant switch
    {
        InstantType.Click => "click",
        InstantType.Dismiss => "dismiss",
        InstantType.Group => "group",
        _ => null
    };

    /// <summary>
    /// Converts a <see cref="CollisionBoundary"/> value to its corresponding data attribute string.
    /// </summary>
    public static string ToDataAttributeString(this CollisionBoundary collisionBoundary) => collisionBoundary switch
    {
        CollisionBoundary.Viewport => "viewport",
        CollisionBoundary.ClippingAncestors => "clipping-ancestors",
        _ => "clipping-ancestors"
    };

    /// <summary>
    /// Converts a <see cref="CollisionAvoidance"/> value to its corresponding data attribute string.
    /// </summary>
    public static string ToDataAttributeString(this CollisionAvoidance collisionAvoidance) => collisionAvoidance switch
    {
        CollisionAvoidance.None => "none",
        CollisionAvoidance.Shift => "shift",
        CollisionAvoidance.Flip => "flip",
        CollisionAvoidance.FlipShift => "flip-shift",
        _ => "flip-shift"
    };
}
