namespace BlazorBaseUI.Tests.Contracts.Dialog;

public interface IDialogViewportContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasRolePresentation();
    Task RendersOnlyWhenMounted();
    Task StaysMountedWithKeepMounted();
}
