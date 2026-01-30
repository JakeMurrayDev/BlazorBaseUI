namespace BlazorBaseUI.Tests.Contracts.Tooltip;

public interface ITooltipTriggerContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task HasAriaDescribedByWhenOpen();
    Task HasDataPopupOpenWhenOpen();
    Task HasDisabledAttributeWhenDisabled();
    Task HasAriaDisabledWhenDisabledAndNotButton();
    Task DoesNotOpenWhenDisabled();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task RequiresContext();
}
