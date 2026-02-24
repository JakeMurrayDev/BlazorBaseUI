namespace BlazorBaseUI.Tests.Contracts.ContextMenu;

public interface IContextMenuRootContract
{
    Task CascadesContextToTrigger();
    Task UncontrolledModeUsesDefaultOpen();
    Task ControlledModeRespectsOpenParameter();
    Task InvokesOnOpenChangeCallback();
    Task DisabledStatePreventsInteraction();
    Task SupportsOrientations();
    Task SetsParentTypeToContextMenu();
    Task OmitsModalOpenOnHoverDelayCloseDelayProps();
}
