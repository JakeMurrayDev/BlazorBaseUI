namespace BlazorBaseUI.Popover;

/// <summary>
/// Provides extension methods for converting popover enumerations to their data attribute string representations.
/// </summary>
internal static class Extensions
{
    /// <summary>
    /// Converts an <see cref="InstantType"/> value to its corresponding data attribute string.
    /// </summary>
    public static string? ToDataAttributeString(this InstantType instant) => instant switch
    {
        InstantType.Click => "click",
        InstantType.Dismiss => "dismiss",
        _ => null
    };

    /// <summary>
    /// Converts a <see cref="CollisionAvoidanceSideMode"/> value to its corresponding JS string.
    /// </summary>
    public static string ToJsString(this CollisionAvoidanceSideMode mode) => mode switch
    {
        CollisionAvoidanceSideMode.None => "none",
        CollisionAvoidanceSideMode.Flip => "flip",
        CollisionAvoidanceSideMode.Shift => "shift",
        _ => "flip"
    };

    /// <summary>
    /// Converts a <see cref="CollisionAvoidanceAlignMode"/> value to its corresponding JS string.
    /// </summary>
    public static string ToJsString(this CollisionAvoidanceAlignMode mode) => mode switch
    {
        CollisionAvoidanceAlignMode.None => "none",
        CollisionAvoidanceAlignMode.Flip => "flip",
        CollisionAvoidanceAlignMode.Shift => "shift",
        _ => "shift"
    };

    /// <summary>
    /// Converts a <see cref="CollisionAvoidanceFallbackAxisSide"/> value to its corresponding JS string.
    /// </summary>
    public static string ToJsString(this CollisionAvoidanceFallbackAxisSide mode) => mode switch
    {
        CollisionAvoidanceFallbackAxisSide.None => "none",
        CollisionAvoidanceFallbackAxisSide.Start => "start",
        CollisionAvoidanceFallbackAxisSide.End => "end",
        _ => "none"
    };
}
