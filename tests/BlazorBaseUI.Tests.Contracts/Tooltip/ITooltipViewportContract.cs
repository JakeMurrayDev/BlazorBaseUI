namespace BlazorBaseUI.Tests.Contracts.Tooltip;

public interface ITooltipViewportContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task RendersChildrenInCurrentContainer();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task RequiresContext();
}
