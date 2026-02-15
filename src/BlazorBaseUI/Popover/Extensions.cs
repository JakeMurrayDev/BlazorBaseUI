namespace BlazorBaseUI.Popover;

/// <summary>
/// Provides extension methods for converting popover enumerations to their data attribute string representations.
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
    /// Converts a <see cref="TransitionStatus"/> value to its corresponding data attribute string.
    /// </summary>
    public static string? ToDataAttributeString(this TransitionStatus status) => status switch
    {
        TransitionStatus.Starting => "starting",
        TransitionStatus.Ending => "ending",
        _ => null
    };

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
    /// Converts a <see cref="CollisionBoundary"/> value to its corresponding data attribute string.
    /// </summary>
    public static string ToDataAttributeString(this CollisionBoundary collisionBoundary) => collisionBoundary switch
    {
        CollisionBoundary.Viewport => "viewport",
        CollisionBoundary.ClippingAncestors => "clipping-ancestors",
        _ => "clipping-ancestors"
    };
}
