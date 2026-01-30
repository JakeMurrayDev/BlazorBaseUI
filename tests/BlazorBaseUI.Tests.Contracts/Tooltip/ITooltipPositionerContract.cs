namespace BlazorBaseUI.Tests.Contracts.Tooltip;

public interface ITooltipPositionerContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task HasRolePresentation();
    Task HasDataSideAttribute();
    Task HasDataAlignAttribute();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task HasHiddenWhenNotMounted();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CascadesPositionerContext();
    Task RequiresContext();
}
