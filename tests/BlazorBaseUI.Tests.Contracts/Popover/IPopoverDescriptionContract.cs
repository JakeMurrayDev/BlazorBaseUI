namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverDescriptionContract
{
    Task RendersAsParagraphByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task SetsAriaDescribedByOnPopup();
    Task RequiresContext();
}
