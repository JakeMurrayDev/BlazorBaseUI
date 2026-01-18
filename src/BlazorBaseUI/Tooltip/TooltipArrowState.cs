using BlazorBaseUI.Popover;

namespace BlazorBaseUI.Tooltip;

public readonly record struct TooltipArrowState(
    bool Open,
    Side Side,
    Align Align,
    bool Uncentered,
    TooltipInstantType Instant);
