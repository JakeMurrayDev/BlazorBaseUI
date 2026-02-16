namespace BlazorBaseUI.Popover;

/// <summary>
/// Specifies the side of the anchor element to position the popover against.
/// </summary>
public enum Side
{
    /// <summary>
    /// Positions the popover above the anchor element.
    /// </summary>
    Top,

    /// <summary>
    /// Positions the popover below the anchor element.
    /// </summary>
    Bottom,

    /// <summary>
    /// Positions the popover to the left of the anchor element.
    /// </summary>
    Left,

    /// <summary>
    /// Positions the popover to the right of the anchor element.
    /// </summary>
    Right,

    /// <summary>
    /// Positions the popover at the inline-end of the anchor element (logical direction).
    /// </summary>
    InlineEnd,

    /// <summary>
    /// Positions the popover at the inline-start of the anchor element (logical direction).
    /// </summary>
    InlineStart
}

/// <summary>
/// Specifies the alignment of the popover relative to the specified side.
/// </summary>
public enum Align
{
    /// <summary>
    /// Aligns the popover to the start edge.
    /// </summary>
    Start,

    /// <summary>
    /// Aligns the popover to the center.
    /// </summary>
    Center,

    /// <summary>
    /// Aligns the popover to the end edge.
    /// </summary>
    End
}

/// <summary>
/// Specifies the modal behavior of the popover.
/// </summary>
public enum ModalMode
{
    /// <summary>
    /// The popover is not modal.
    /// </summary>
    False,

    /// <summary>
    /// The popover is fully modal with a backdrop and focus trap.
    /// </summary>
    True,

    /// <summary>
    /// The popover traps focus without rendering a backdrop.
    /// </summary>
    TrapFocus
}

/// <summary>
/// Specifies the current transition status of a popover component.
/// </summary>
public enum TransitionStatus
{
    /// <summary>
    /// The component is in the starting phase of a transition.
    /// </summary>
    Starting,

    /// <summary>
    /// The component is in the ending phase of a transition.
    /// </summary>
    Ending,

    /// <summary>
    /// No transition is currently active.
    /// </summary>
    None
}

/// <summary>
/// Specifies the type of instant (non-animated) transition.
/// </summary>
public enum InstantType
{
    /// <summary>
    /// No instant transition is in effect.
    /// </summary>
    None,

    /// <summary>
    /// The popover was opened via a click interaction.
    /// </summary>
    Click,

    /// <summary>
    /// The popover was dismissed.
    /// </summary>
    Dismiss
}

/// <summary>
/// Specifies the reason for a popover open state change.
/// </summary>
public enum OpenChangeReason
{
    /// <summary>
    /// The popover was opened or closed by hovering over the trigger.
    /// </summary>
    TriggerHover,

    /// <summary>
    /// The popover was opened or closed by focusing the trigger.
    /// </summary>
    TriggerFocus,

    /// <summary>
    /// The popover was opened or closed by pressing the trigger.
    /// </summary>
    TriggerPress,

    /// <summary>
    /// The popover was closed by pressing outside of it.
    /// </summary>
    OutsidePress,

    /// <summary>
    /// The popover was closed by pressing the Escape key.
    /// </summary>
    EscapeKey,

    /// <summary>
    /// The popover was closed by pressing the close button.
    /// </summary>
    ClosePress,

    /// <summary>
    /// The popover was closed because focus moved outside of it.
    /// </summary>
    FocusOut,

    /// <summary>
    /// The popover was opened or closed via an imperative API call.
    /// </summary>
    ImperativeAction,

    /// <summary>
    /// No specific reason was provided.
    /// </summary>
    None
}

/// <summary>
/// Specifies the CSS positioning method for the popover.
/// </summary>
public enum PositionMethod
{
    /// <summary>
    /// Uses CSS <c>position: absolute</c> for positioning.
    /// </summary>
    Absolute,

    /// <summary>
    /// Uses CSS <c>position: fixed</c> for positioning.
    /// </summary>
    Fixed
}

/// <summary>
/// Specifies the boundary used to detect collisions for repositioning.
/// </summary>
public enum CollisionBoundary
{
    /// <summary>
    /// Uses the viewport as the collision boundary.
    /// </summary>
    Viewport,

    /// <summary>
    /// Uses the clipping ancestors of the popover as the collision boundary.
    /// </summary>
    ClippingAncestors
}

/// <summary>
/// Specifies how side collisions are handled when positioning the popup.
/// </summary>
public enum CollisionAvoidanceSideMode
{
    /// <summary>
    /// No collision avoidance on the side axis.
    /// </summary>
    None,

    /// <summary>
    /// Flips to the opposite side when a collision is detected.
    /// </summary>
    Flip,

    /// <summary>
    /// Shifts along the side axis to avoid the collision.
    /// </summary>
    Shift
}

/// <summary>
/// Specifies how alignment collisions are handled when positioning the popup.
/// </summary>
public enum CollisionAvoidanceAlignMode
{
    /// <summary>
    /// No collision avoidance on the alignment axis.
    /// </summary>
    None,

    /// <summary>
    /// Flips the alignment when a collision is detected.
    /// </summary>
    Flip,

    /// <summary>
    /// Shifts along the alignment axis to avoid the collision.
    /// </summary>
    Shift
}

/// <summary>
/// Specifies the fallback axis side when the popup cannot avoid a collision on both axes.
/// </summary>
public enum CollisionAvoidanceFallbackAxisSide
{
    /// <summary>
    /// No fallback axis side.
    /// </summary>
    None,

    /// <summary>
    /// Falls back to the start of the axis.
    /// </summary>
    Start,

    /// <summary>
    /// Falls back to the end of the axis.
    /// </summary>
    End
}

/// <summary>
/// Determines how to handle collisions when positioning the popup.
/// </summary>
public sealed class CollisionAvoidance
{
    /// <summary>
    /// Gets or sets how side collisions are handled.
    /// When <see cref="CollisionAvoidanceSideMode.Flip"/>, the popup flips to the opposite side.
    /// When <see cref="CollisionAvoidanceSideMode.Shift"/>, the popup shifts along the side axis.
    /// </summary>
    public CollisionAvoidanceSideMode Side { get; set; } = CollisionAvoidanceSideMode.Flip;

    /// <summary>
    /// Gets or sets how alignment collisions are handled.
    /// When <see cref="CollisionAvoidanceAlignMode.Flip"/>, the alignment flips.
    /// When <see cref="CollisionAvoidanceAlignMode.Shift"/>, the popup shifts along the alignment axis.
    /// </summary>
    public CollisionAvoidanceAlignMode Align { get; set; } = CollisionAvoidanceAlignMode.Shift;

    /// <summary>
    /// Gets or sets the fallback axis side used when collisions cannot be fully avoided.
    /// </summary>
    public CollisionAvoidanceFallbackAxisSide FallbackAxisSide { get; set; } = CollisionAvoidanceFallbackAxisSide.None;
}
