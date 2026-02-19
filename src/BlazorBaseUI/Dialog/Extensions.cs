namespace BlazorBaseUI.Dialog;

/// <summary>
/// Extension methods for converting dialog enumerations to data attribute strings.
/// </summary>
internal static class Extensions
{
    public static string? ToDataAttributeString(this InstantType instant) => instant switch
    {
        InstantType.Click => "click",
        InstantType.Dismiss => "dismiss",
        _ => null
    };

    public static string? ToDataAttributeString(this ModalMode mode) => mode switch
    {
        ModalMode.False => "false",
        ModalMode.True => "true",
        ModalMode.TrapFocus => "trap-focus",
        _ => null
    };

    public static string ToRoleString(this DialogRole role) => role switch
    {
        DialogRole.AlertDialog => "alertdialog",
        _ => "dialog"
    };
}
