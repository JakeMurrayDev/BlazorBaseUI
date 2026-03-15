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
        InstantType.Focus => "focus",
        InstantType.TriggerChange => "trigger-change",
        _ => null
    };

    /// <summary>
    /// Parses a side string from FloatingUI into the corresponding <see cref="Side"/> enum value.
    /// </summary>
    public static Side ParseSide(string value) => value switch
    {
        "top" => Side.Top,
        "right" => Side.Right,
        "bottom" => Side.Bottom,
        "left" => Side.Left,
        _ => Side.Bottom
    };

    /// <summary>
    /// Parses an align string from FloatingUI into the corresponding <see cref="Align"/> enum value.
    /// </summary>
    public static Align ParseAlign(string value) => value switch
    {
        "start" => Align.Start,
        "center" => Align.Center,
        "end" => Align.End,
        _ => Align.Center
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
