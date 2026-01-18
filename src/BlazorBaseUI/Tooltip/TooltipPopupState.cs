using BlazorBaseUI.Popover;

namespace BlazorBaseUI.Tooltip;

public readonly record struct TooltipPopupState(
    bool Open,
    Side Side,
    Align Align,
    TooltipInstantType Instant,
    Popover.TransitionStatus TransitionStatus);
