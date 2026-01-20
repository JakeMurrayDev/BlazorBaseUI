namespace BlazorBaseUI.Dialog;

public readonly record struct DialogViewportState(bool Open, TransitionStatus TransitionStatus, bool Nested, bool NestedDialogOpen);
