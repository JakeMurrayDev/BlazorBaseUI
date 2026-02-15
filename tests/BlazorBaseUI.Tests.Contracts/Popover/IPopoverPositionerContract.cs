namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverPositionerContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasDataSideAttribute();
    Task HasDataAlignAttribute();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task RequiresContext();
}
