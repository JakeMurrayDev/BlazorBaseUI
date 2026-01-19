namespace BlazorBaseUI.Tooltip;

public enum TrackCursorAxis
{
    None,
    X,
    Y,
    Both
}

public enum TooltipInstantType
{
    None,
    Delay,
    Focus,
    Dismiss,
    TrackingCursor
}

public enum TooltipOpenChangeReason
{
    None,
    TriggerHover,
    TriggerFocus,
    TriggerPress,
    OutsidePress,
    EscapeKey,
    Disabled,
    ImperativeAction
}
