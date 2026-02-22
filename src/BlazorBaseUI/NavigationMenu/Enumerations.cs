namespace BlazorBaseUI.NavigationMenu;

/// <summary>
/// Specifies the visual orientation of the navigation menu.
/// </summary>
public enum NavigationMenuOrientation
{
    /// <summary>Horizontal orientation; items are laid out left to right.</summary>
    Horizontal,

    /// <summary>Vertical orientation; items are laid out top to bottom.</summary>
    Vertical
}

/// <summary>
/// Specifies the direction from which a navigation menu item was activated,
/// used to drive enter/exit animations.
/// </summary>
public enum ActivationDirection
{
    /// <summary>No activation direction.</summary>
    None,

    /// <summary>Activated from the left.</summary>
    Left,

    /// <summary>Activated from the right.</summary>
    Right,

    /// <summary>Activated from above.</summary>
    Up,

    /// <summary>Activated from below.</summary>
    Down
}

/// <summary>
/// Specifies which side of the anchor element to align the popup against.
/// </summary>
public enum Side
{
    /// <summary>The top side of the anchor element.</summary>
    Top,

    /// <summary>The bottom side of the anchor element.</summary>
    Bottom,

    /// <summary>The left side of the anchor element.</summary>
    Left,

    /// <summary>The right side of the anchor element.</summary>
    Right,

    /// <summary>The inline-end side of the anchor element.</summary>
    InlineEnd,

    /// <summary>The inline-start side of the anchor element.</summary>
    InlineStart
}

/// <summary>
/// Specifies how to align the popup relative to the specified side.
/// </summary>
public enum Align
{
    /// <summary>Aligns the popup to the start of the anchor.</summary>
    Start,

    /// <summary>Aligns the popup to the center of the anchor.</summary>
    Center,

    /// <summary>Aligns the popup to the end of the anchor.</summary>
    End
}

/// <summary>
/// Represents the current transition animation status of a navigation menu element.
/// </summary>
public enum TransitionStatus
{
    /// <summary>The element is animating in.</summary>
    Starting,

    /// <summary>The element is animating out.</summary>
    Ending,

    /// <summary>No transition is active.</summary>
    None
}

/// <summary>
/// Specifies the type of instant transition to apply.
/// </summary>
public enum InstantType
{
    /// <summary>No instant transition.</summary>
    None,

    /// <summary>Instant transition triggered by a click interaction.</summary>
    Click,

    /// <summary>Instant transition triggered by a dismiss interaction.</summary>
    Dismiss
}

/// <summary>
/// Specifies which CSS <c>position</c> property to use for the positioner.
/// </summary>
public enum PositionMethod
{
    /// <summary>Uses CSS <c>position: absolute</c>.</summary>
    Absolute,

    /// <summary>Uses CSS <c>position: fixed</c>.</summary>
    Fixed
}

/// <summary>
/// Specifies the collision boundary used to determine repositioning behavior.
/// </summary>
public enum CollisionBoundary
{
    /// <summary>Uses the viewport as the collision boundary.</summary>
    Viewport,

    /// <summary>Uses the clipping ancestors as the collision boundary.</summary>
    ClippingAncestors
}

/// <summary>
/// Specifies how to handle collisions when positioning the popup.
/// </summary>
public enum CollisionAvoidance
{
    /// <summary>No collision avoidance is applied.</summary>
    None,

    /// <summary>Shifts the popup along the axis to stay within the boundary.</summary>
    Shift,

    /// <summary>Flips the popup to the opposite side to stay within the boundary.</summary>
    Flip,

    /// <summary>Flips the popup to the opposite side and shifts it along the axis to stay within the boundary.</summary>
    FlipShift
}
