namespace BlazorBaseUI.Tests.Contracts.Menu;

public interface IMenuRadioItemContract
{
    Task HasRoleMenuitemradio();
    Task HasAriaCheckedWhenSelected();
    Task HasDataCheckedWhenSelected();
    Task SelectsOnClick();
    Task InheritsDisabledFromGroup();
}
