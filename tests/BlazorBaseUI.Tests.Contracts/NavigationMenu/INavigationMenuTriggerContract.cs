namespace BlazorBaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuTriggerContract
{
    Task RendersButtonByDefault();
    Task ForwardsAdditionalAttributes();
    Task HasAriaExpandedFalse();
    Task HasAriaExpandedTrue();
    Task NoAriaControlsWhenClosed();
    Task HasAriaControlsWhenOpen();
    Task HasTabIndex();
    Task NoDataPopupOpenWhenClosed();
    Task HasDataPopupOpenWhenOpen();
    Task AppliesClassValue();
    Task ToggleOnClick();
    Task DoesNotToggleWhenDisabled();
    Task RequiresContext();
}
