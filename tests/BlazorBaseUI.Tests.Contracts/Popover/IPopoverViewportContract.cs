namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverViewportContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task RendersChildrenInCurrentContainer();
    Task HasDataCurrentAttribute();
    Task RequiresContext();
}
