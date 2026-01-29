namespace BlazorBaseUI.Tests.Contracts.Menu;

public interface IMenuRadioGroupContract
{
    Task HasRoleGroup();
    Task CascadesContextToRadioItems();
    Task ControlledModeRespectsValueParameter();
    Task UncontrolledModeUsesDefaultValue();
    Task InvokesOnValueChange();
    Task SupportsCancelInOnValueChange();
}
