namespace BlazorBaseUI.Tests.Contracts.Tooltip;

public interface ITooltipArrowContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task HasAriaHiddenTrue();
    Task HasDataSideAttribute();
    Task HasDataAlignAttribute();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task RequiresContext();
}
