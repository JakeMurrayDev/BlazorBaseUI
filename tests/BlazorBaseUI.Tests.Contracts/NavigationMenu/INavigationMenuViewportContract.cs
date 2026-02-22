namespace BlazorBaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuViewportContract
{
    Task RendersDivByDefault();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task RequiresContext();
}
