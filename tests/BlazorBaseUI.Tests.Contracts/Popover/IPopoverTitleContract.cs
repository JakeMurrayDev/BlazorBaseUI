namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverTitleContract
{
    Task RendersAsH2ByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task SetsAriaLabelledByOnPopup();
    Task RendersWithDefaultContext();
}
