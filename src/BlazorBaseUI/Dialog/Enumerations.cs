namespace BlazorBaseUI.Dialog;

public enum OpenChangeReason
{
    None,
    TriggerPress,
    OutsidePress,
    EscapeKey,
    ClosePress,
    ImperativeAction
}

public enum InstantType
{
    None,
    Click,
    Dismiss
}

public enum ModalMode
{
    False,
    True,
    TrapFocus
}

public enum DialogRole
{
    Dialog,
    AlertDialog
}
