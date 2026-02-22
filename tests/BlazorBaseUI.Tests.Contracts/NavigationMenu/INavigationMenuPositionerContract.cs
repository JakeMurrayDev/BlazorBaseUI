namespace BlazorBaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuPositionerContract
{
    Task RendersDivByDefault();
    Task ForwardsAdditionalAttributes();
    Task HasRolePresentation();
    Task HasDataSide();
    Task HasDataAlign();
    Task AppliesClassValue();
    Task RequiresContext();
}
