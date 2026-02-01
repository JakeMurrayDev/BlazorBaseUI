namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverBackdropContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task HasDataOpenWhenOpen();
    Task RequiresContext();
}
