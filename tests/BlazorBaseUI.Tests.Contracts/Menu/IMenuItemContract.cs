namespace BlazorBaseUI.Tests.Contracts.Menu;

public interface IMenuItemContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task HasRoleMenuitem();
    Task HasTabindexMinusOneByDefault();
    Task HasDataDisabledWhenDisabled();
    Task HasAriaDisabledWhenDisabled();
    Task InvokesOnClickHandler();
    Task ClosesMenuOnClickByDefault();
    Task DoesNotCloseWhenCloseOnClickFalse();
    Task DoesNotActivateWhenDisabled();
}
