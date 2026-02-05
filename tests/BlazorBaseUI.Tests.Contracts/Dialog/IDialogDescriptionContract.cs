namespace BlazorBaseUI.Tests.Contracts.Dialog;

public interface IDialogDescriptionContract
{
    Task RendersAsParagraphByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task GeneratesIdForAriaDescribedBy();
}
