namespace BlazorBaseUI.Tests.Contracts;

public interface ICollapsibleRootContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task CascadesContextToChildren();
    Task ControlledModeRespectsOpenParameter();
    Task UncontrolledModeUsesDefaultOpen();
    Task InvokesOnOpenChange();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task HasDataDisabledWhenDisabled();
}
