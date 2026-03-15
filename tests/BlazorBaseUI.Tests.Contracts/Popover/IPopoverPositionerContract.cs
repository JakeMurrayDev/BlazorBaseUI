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
    Task ForwardsSideOffsetToInitializePositioner();
    Task ForwardsAlignOffsetToInitializePositioner();
    Task UpdatesDataSideFromJsCallback();
    Task UpdatesDataAlignFromJsCallback();
    Task SetsDataAnchorHiddenFromJsCallback();
    Task RendersInternalBackdropWhenModalAndPressed();
    Task DoesNotRenderInternalBackdropWhenNotModal();
    Task DoesNotRenderInternalBackdropWhenHoverOpened();
}
