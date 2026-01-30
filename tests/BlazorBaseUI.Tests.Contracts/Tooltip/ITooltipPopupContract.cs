namespace BlazorBaseUI.Tests.Contracts.Tooltip;

public interface ITooltipPopupContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task HasRoleTooltip();
    Task HasDataSideAttribute();
    Task HasDataAlignAttribute();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task RendersChildren();
    Task RequiresContext();
}
