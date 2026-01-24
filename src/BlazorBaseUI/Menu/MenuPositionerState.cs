namespace BlazorBaseUI.Menu;

public readonly record struct MenuPositionerState(
    bool Open,
    Side Side,
    Align Align,
    bool AnchorHidden,
    bool Nested);
