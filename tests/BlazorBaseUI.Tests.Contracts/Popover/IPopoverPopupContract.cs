namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverPopupContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task RendersChildren();
    Task HasRoleDialog();
    Task HasAriaModal();
    Task HasDataOpenWhenOpen();
    Task HasAriaLabelledByWhenTitlePresent();
    Task HasAriaDescribedByWhenDescriptionPresent();
    Task AppliesClassValueWithState();
    Task AppliesStyleValueWithState();
    Task RequiresContext();
}
