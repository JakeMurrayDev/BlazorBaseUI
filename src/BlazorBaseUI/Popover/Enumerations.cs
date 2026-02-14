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
