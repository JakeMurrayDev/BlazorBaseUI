namespace BlazorBaseUI.Tests.Contracts.MenuBar;

public interface IMenuBarRootContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task HasRoleMenubar();
    Task HasAriaOrientationHorizontalByDefault();
    Task HasAriaOrientationVerticalWhenSet();
    Task HasDataOrientationAttribute();
    Task HasDataDisabledWhenDisabled();
    Task CascadesContextToChildren();
    Task TracksHasSubmenuOpenState();
}
