namespace BlazorBaseUI.Tests.Contracts.Dialog;

public interface IDialogRootContract
{
    Task RendersChildren();
    Task OpensByDefaultWhenDefaultOpenTrue();
    Task RemainsClosedWhenControlledOpenFalse();
    Task SetsAriaLabelledByFromTitle();
    Task SetsAriaDescribedByFromDescription();
    Task CallsOnOpenChangeWhenOpenStateChanges();
    Task OnOpenChangeReasonTriggerPress();
    Task OnOpenChangeReasonClosePress();
    Task OnOpenChangeCancelPreventsOpening();
    Task RendersInternalBackdropWhenModalTrue();
    Task DoesNotRenderInternalBackdropWhenModalFalse();
    Task ActionsRefCloseMethodClosesDialog();
    Task ActionsRefUnmountMethodUnmountsDialog();
    Task ActionsRefOpenMethodOpensDialog();
}
