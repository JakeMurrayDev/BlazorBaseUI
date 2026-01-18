namespace BlazorBaseUI.Popover;

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
    True,
    TrapFocus
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
    Dismiss
}

public enum OpenChangeReason
{
    TriggerHover,
    TriggerFocus,
    TriggerPress,
    OutsidePress,
    EscapeKey,
    ClosePress,
    FocusOut,
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
