using BlazorBaseUI.Popover;

namespace BlazorBaseUI.Tooltip;

public readonly record struct TooltipPositionerState(
    bool Open,
    Side Side,
    Align Align,
    bool AnchorHidden,
    TooltipInstantType Instant);
