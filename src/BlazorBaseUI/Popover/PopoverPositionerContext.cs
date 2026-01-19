using Microsoft.AspNetCore.Components;

namespace BlazorBaseUI.Popover;

internal sealed record PopoverPositionerContext
{
    public PopoverPositionerContext(
        Side side,
        Align align,
        bool anchorHidden,
        bool arrowUncentered,
        Func<ElementReference?> getArrowElement,
        Action<ElementReference?> setArrowElement)
    {
        Side = side;
        Align = align;
        AnchorHidden = anchorHidden;
        ArrowUncentered = arrowUncentered;
        GetArrowElement = getArrowElement;
        SetArrowElement = setArrowElement;
    }

    public Side Side { get; set; }

    public Align Align { get; set; }

    public bool AnchorHidden { get; set; }

    public bool ArrowUncentered { get; set; }

    public Func<ElementReference?> GetArrowElement { get; }

    public Action<ElementReference?> SetArrowElement { get; }
}
