namespace BlazorBaseUI.Menu;

public readonly record struct MenuBackdropState(
    bool Open,
    TransitionStatus TransitionStatus);
