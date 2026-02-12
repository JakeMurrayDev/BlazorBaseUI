namespace BlazorBaseUI.Tests.Contracts.Menu;

public interface IMenuCheckboxItemContract
{
    Task HasRoleMenuitemcheckbox();
    Task RendersWithCustomRender();
    Task HasAriaCheckedFalseWhenUnchecked();
    Task HasAriaCheckedTrueWhenChecked();
    Task HasDataCheckedWhenChecked();
    Task HasDataUncheckedWhenUnchecked();
    Task ControlledModeRespectsCheckedParameter();
    Task UncontrolledModeUsesDefaultChecked();
    Task TogglesOnClick();
    Task InvokesOnCheckedChange();
    Task SupportsCancelInOnCheckedChange();
}
