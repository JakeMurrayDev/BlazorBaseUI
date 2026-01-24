namespace BlazorBaseUI.Menu;

public readonly record struct MenuPopupState(
    bool Open,
    Side Side,
    Align Align,
    InstantType Instant,
    TransitionStatus TransitionStatus,
    bool Nested);
