namespace BlazorBaseUI.Tests.Contracts.Radio;

public interface IRadioIndicatorContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // Visibility
    Task DoesNotRenderByDefault();
    Task RendersWhenChecked();

    // KeepMounted
    Task KeepsIndicatorMountedWhenUnchecked();
    Task KeepsIndicatorMountedWhenChecked();

    // Style hooks
    Task HasDataCheckedWhenChecked();
    Task HasDataUncheckedWhenUncheckedAndKeepMounted();
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadonlyWhenReadOnly();
    Task HasDataRequiredWhenRequired();
    Task TransitionStatusAttributes();

    // Context
    Task ReceivesStateFromContext();
    Task HandlesNullContext();

    // State
    Task ClassValueReceivesCorrectState();
    Task StyleValueReceivesCorrectState();

    // RenderAs validation
    Task ThrowsWhenRenderAsDoesNotImplementInterface();
}
