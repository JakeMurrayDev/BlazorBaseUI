namespace BlazorBaseUI.Tests.Contracts.Switch;

public interface ISwitchThumbContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // Style hooks (data attributes)
    Task HasDataCheckedWhenChecked();
    Task HasDataUncheckedWhenUnchecked();
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadonlyWhenReadOnly();
    Task HasDataRequiredWhenRequired();

    // Context
    Task ReceivesStateFromContext();
    Task HandlesNullContext();

    // State
    Task ClassValueReceivesCorrectState();
    Task StyleValueReceivesCorrectState();

    // RenderAs validation
    Task ThrowsWhenRenderAsDoesNotImplementInterface();
}
