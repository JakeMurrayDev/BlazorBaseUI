namespace BlazorBaseUI.Dialog;

public readonly record struct DialogPopupState(bool Open, TransitionStatus TransitionStatus, InstantType Instant, int NestedDialogCount);
