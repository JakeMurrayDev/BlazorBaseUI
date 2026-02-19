namespace BlazorBaseUI.Tests.Contracts.MenuBar;

public interface IMenuBarRootContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task HasRoleMenubar();
    Task HasAriaOrientationHorizontalByDefault();
    Task HasAriaOrientationVerticalWhenSet();
    Task HasDataOrientationAttribute();
    Task HasDataDisabledWhenDisabled();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CascadesContextToChildren();
    Task TracksHasSubmenuOpenState();
}
