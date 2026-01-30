namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverArrowContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task HasDataSideAttribute();
    Task RequiresContext();
}
