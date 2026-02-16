namespace BlazorBaseUI.Dialog;

/// <summary>
/// Represents the state of the <see cref="DialogRoot"/> component.
/// </summary>
/// <param name="Open">Indicates whether the dialog is currently open.</param>
/// <param name="NestedDialogCount">The number of nested dialogs currently open within this dialog.</param>
public readonly record struct DialogRootState(bool Open, int NestedDialogCount);
