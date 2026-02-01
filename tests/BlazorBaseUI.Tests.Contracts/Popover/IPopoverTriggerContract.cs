namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverTriggerContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task HasAriaHaspopupDialog();
    Task HasAriaExpandedFalseWhenClosed();
    Task HasAriaExpandedTrueWhenOpen();
    Task HasDataPopupOpenWhenOpen();
    Task HasDisabledAttributeWhenDisabled();
    Task DoesNotToggleWhenDisabled();
    Task TogglesOnClick();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task RequiresContext();
}
