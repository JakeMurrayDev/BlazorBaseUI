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
}
