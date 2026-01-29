namespace BlazorBaseUI.Tests.Contracts;

public interface ICollapsibleTriggerContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasAriaExpandedFalseWhenClosed();
    Task HasAriaExpandedTrueWhenOpen();
    Task HasAriaControlsWhenOpen();
    Task HasNoAriaControlsWhenClosed();
    Task HasDataPanelOpenWhenOpen();
    Task HasNoDataPanelOpenWhenClosed();
    Task HasDataDisabledWhenDisabled();
    Task HasDisabledAttributeWhenDisabled();
    Task TogglesOnClick();
    Task DoesNotToggleWhenDisabled();
    Task RequiresContext();
}
