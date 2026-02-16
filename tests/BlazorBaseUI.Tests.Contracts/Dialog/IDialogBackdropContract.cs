namespace BlazorBaseUI.Tests.Contracts.Dialog;

public interface IDialogBackdropContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasRolePresentation();
    Task DoesNotRenderWhenModalFalse();
    Task ForceRenderOnlyRootByDefault();
    Task ForceRenderAllWhenTrue();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
}
