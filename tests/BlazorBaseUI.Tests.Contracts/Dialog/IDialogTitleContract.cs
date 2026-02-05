namespace BlazorBaseUI.Tests.Contracts.Dialog;

public interface IDialogTitleContract
{
    Task RendersAsH2ByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task GeneratesIdForAriaLabelledBy();
}
