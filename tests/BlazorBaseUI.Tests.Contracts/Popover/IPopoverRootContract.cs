namespace BlazorBaseUI.Tests.Contracts.Popover;

public interface IPopoverRootContract
{
    Task RendersChildren();
    Task OpensByDefaultWhenDefaultOpenTrue();
    Task RemainsClosedWhenDefaultOpenFalseControlled();
    Task ClosesWhenTriggerClickedTwice();
    Task CallsOnOpenChangeWhenOpenStateChanges();
    Task OnOpenChangeCancelPreventsOpening();
    Task RendersInternalBackdropWhenModalTrue();
    Task DoesNotRenderInternalBackdropWhenModalFalse();
    Task ActionsRefCloseMethodClosesPopover();
    Task ActionsRefUnmountMethodUnmountsPopover();
    Task OnOpenChangeCompleteNotCalledOnMount();
    Task MultiTrigger_OpensWithAnyContainedTrigger();
    Task Handle_OpensAndClosesImperatively();
    Task Handle_SetsPayload();
    Task ResetsOpenChangeReasonOnTransitionEnd();
    Task ResetsOpenChangeReasonOnForceUnmount();
    Task SetsInstantClickOnlyForKeyboardTriggerPress();
    Task DoesNotSetInstantDismissOnOutsidePressClose();
    Task DoesNotSetInstantClickOnClosePressClose();
    Task ViewportTriggerSwapClearsInstantType();
    Task PreventUnmountOnCloseKeepsPopupMounted();
    Task PreventUnmountOnCloseFlagIsResetOnNextClose();
    Task ScrollLockReactsToModalParameterChange();
    Task InteractionTypeClearedOnTransitionEnd();
}
