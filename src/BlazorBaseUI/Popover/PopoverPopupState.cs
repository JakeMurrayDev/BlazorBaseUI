namespace BlazorBaseUI.Popover;

public readonly record struct PopoverPopupState(bool Open, Side Side, Align Align, InstantType Instant, TransitionStatus TransitionStatus);
