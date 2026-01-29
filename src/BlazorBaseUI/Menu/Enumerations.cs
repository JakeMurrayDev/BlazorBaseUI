namespace BlazorBaseUI.Menu;

public enum Side
{
    Top,
    Bottom,
    Left,
    Right,
    InlineEnd,
    InlineStart
}

public enum Align
{
    Start,
    Center,
    End
}

public enum ModalMode
{
    False,
    True
}

public enum TransitionStatus
{
    Starting,
    Ending,
    None
}

public enum InstantType
{
    None,
    Click,
    Dismiss,
    Group
}

public enum OpenChangeReason
{
    TriggerHover,
    TriggerFocus,
    TriggerPress,
    OutsidePress,
    EscapeKey,
    ItemPress,
    ClosePress,
    FocusOut,
    ListNavigation,
    SiblingOpen,
    CancelOpen,
    ImperativeAction,
    None
}

public enum PositionMethod
{
    Absolute,
    Fixed
}

public enum CollisionBoundary
{
    Viewport,
    ClippingAncestors
}

public enum MenuParentType
{
    None,
    Menu,
    ContextMenu,
    Menubar
}

public enum MenuOrientation
{
    Vertical,
    Horizontal
}

public enum CollisionAvoidance
{
    None,
    Shift,
    Flip,
    FlipShift
}

public enum TextDirection
{
    Ltr,
    Rtl
}
