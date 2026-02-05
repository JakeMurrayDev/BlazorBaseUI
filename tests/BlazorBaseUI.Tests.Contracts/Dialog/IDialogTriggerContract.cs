namespace BlazorBaseUI.Tests.Contracts.Dialog;

public interface IDialogTriggerContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task DisabledPreventsOpening();
    Task DisabledCustomElement();
    Task HasAriaHasPopupDialog();
    Task HasAriaExpandedFalseWhenClosed();
    Task HasAriaExpandedTrueWhenOpen();
}
