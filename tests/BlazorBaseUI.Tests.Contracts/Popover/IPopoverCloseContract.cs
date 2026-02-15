namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverCloseContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task HasTypeButtonAttribute();
    Task ClosesPopoverOnClick();
    Task RequiresContext();
}
