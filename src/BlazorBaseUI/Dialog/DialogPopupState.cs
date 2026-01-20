namespace BlazorBaseUI.Dialog;

public readonly record struct DialogPopupState(bool Open, TransitionStatus TransitionStatus, bool Nested, bool NestedDialogOpen);
