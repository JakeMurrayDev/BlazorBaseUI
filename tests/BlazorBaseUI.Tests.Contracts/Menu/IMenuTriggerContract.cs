namespace BlazorBaseUI.Tests.Contracts.Menu;

public interface IMenuTriggerContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasAriaHaspopupMenu();
    Task HasAriaExpandedFalseWhenClosed();
    Task HasAriaExpandedTrueWhenOpen();
    Task HasDataPopupOpenWhenOpen();
    Task HasDataDisabledWhenDisabled();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task ToggleMenuOnClick();
    Task DoesNotToggleWhenDisabled();
    Task RequiresContext();
}
