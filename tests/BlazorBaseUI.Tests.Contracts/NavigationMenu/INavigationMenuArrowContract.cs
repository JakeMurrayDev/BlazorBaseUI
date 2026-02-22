namespace BlazorBaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuArrowContract
{
    Task RendersDivByDefault();
    Task ForwardsAdditionalAttributes();
    Task HasAriaHidden();
    Task HasDataSide();
    Task HasDataAlign();
    Task AppliesClassValue();
    Task RequiresContext();
}
