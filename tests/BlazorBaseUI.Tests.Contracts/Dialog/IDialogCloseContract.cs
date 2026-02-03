namespace BlazorBaseUI.Tests.Contracts.Dialog;

public interface IDialogCloseContract
{
    Task RendersAsButtonByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task DisabledPreventsClosing();
    Task DisabledCustomElement();
    Task ClosesDialogOnClick();
    Task ClosesWithUndefinedOnClick();
}
