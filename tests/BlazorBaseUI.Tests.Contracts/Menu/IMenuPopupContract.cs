namespace BlazorBaseUI.Tests.Contracts.Menu;

public interface IMenuPopupContract
{
    Task DefaultReturnFocusIsTrue();
    Task FinalFocusNoneDisablesReturnFocus();
    Task FinalFocusDefaultEnablesReturnFocus();
}
