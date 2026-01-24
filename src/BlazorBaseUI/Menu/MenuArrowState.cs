namespace BlazorBaseUI.Menu;

public readonly record struct MenuArrowState(
    bool Open,
    Side Side,
    Align Align,
    bool Uncentered);
