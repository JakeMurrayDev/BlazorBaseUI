namespace BlazorBaseUI.Dialog;

public readonly record struct DialogRootState(bool Open, int NestedDialogCount);
