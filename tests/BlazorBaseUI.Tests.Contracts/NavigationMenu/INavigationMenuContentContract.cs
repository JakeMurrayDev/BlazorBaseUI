namespace BlazorBaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuContentContract
{
    Task RendersDivByDefault();
    Task ForwardsAdditionalAttributes();
    Task HasDataOpenWhenActive();
    Task HasDataClosedWhenInactive();
    Task AppliesClassValue();
    Task RequiresContext();
}
