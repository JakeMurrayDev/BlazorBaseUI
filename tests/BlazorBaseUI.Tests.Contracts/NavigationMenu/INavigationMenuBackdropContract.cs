namespace BlazorBaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuBackdropContract
{
    Task RendersDivByDefault();
    Task ForwardsAdditionalAttributes();
    Task HasRolePresentation();
    Task HasDataClosedWhenClosed();
    Task AppliesClassValue();
    Task RequiresContext();
}
