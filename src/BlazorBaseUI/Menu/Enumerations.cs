namespace BlazorBaseUI.Menu;

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
/// Specifies the modal behavior of the menu when open.
/// </summary>
public enum ModalMode
{
    /// <summary>The menu is not modal; user interaction with the rest of the document is allowed.</summary>
    False,

    /// <summary>The menu is modal; page scroll is locked and pointer interactions on outside elements are disabled.</summary>
    True
}

/// <summary>
/// Represents the current transition animation status of a menu element.
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
    Dismiss,

    /// <summary>Instant transition triggered by a group interaction.</summary>
    Group
}

/// <summary>
/// Specifies the reason the menu's open state changed.
/// </summary>
public enum OpenChangeReason
{
    /// <summary>The trigger was hovered.</summary>
    TriggerHover,

    /// <summary>The trigger received focus.</summary>
    TriggerFocus,

    /// <summary>The trigger was pressed.</summary>
    TriggerPress,

    /// <summary>A click occurred outside the menu.</summary>
    OutsidePress,

    /// <summary>The Escape key was pressed.</summary>
    EscapeKey,

    /// <summary>A menu item was pressed.</summary>
    ItemPress,

    /// <summary>A close button was pressed.</summary>
    ClosePress,

    /// <summary>Focus moved outside the menu.</summary>
    FocusOut,

    /// <summary>Keyboard list navigation occurred.</summary>
    ListNavigation,

    /// <summary>A sibling menu was opened.</summary>
    SiblingOpen,

    /// <summary>The open action was canceled.</summary>
    CancelOpen,

    /// <summary>An imperative action triggered the change.</summary>
    ImperativeAction,

    /// <summary>No specific reason.</summary>
    None
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
/// Specifies the type of the menu's parent container.
/// </summary>
public enum MenuParentType
{
    /// <summary>The menu has no parent container.</summary>
    None,

    /// <summary>The menu is nested inside another menu.</summary>
    Menu,

    /// <summary>The menu is nested inside a context menu.</summary>
    ContextMenu,

    /// <summary>The menu is nested inside a menubar.</summary>
    Menubar
}

/// <summary>
/// Specifies the visual orientation of the menu.
/// </summary>
public enum MenuOrientation
{
    /// <summary>Vertical orientation; roving focus uses up/down arrow keys.</summary>
    Vertical,

    /// <summary>Horizontal orientation; roving focus uses left/right arrow keys.</summary>
    Horizontal
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

/// <summary>
/// Specifies the text direction of the menu.
/// </summary>
public enum TextDirection
{
    /// <summary>Left-to-right text direction.</summary>
    Ltr,

    /// <summary>Right-to-left text direction.</summary>
    Rtl
}
