using BlazorBaseUI.Popover;
using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Tooltip;

internal sealed record TooltipPositionerContext(
    Side Side,
    Align Align,
    bool AnchorHidden,
    bool ArrowUncentered,
    Func<ElementReference?> GetArrowElement,
    Action<ElementReference?> SetArrowElement)
{
    public Side Side { get; set; } = Side;

    public Align Align { get; set; } = Align;

    public bool AnchorHidden { get; set; } = AnchorHidden;

    public bool ArrowUncentered { get; set; } = ArrowUncentered;
}
