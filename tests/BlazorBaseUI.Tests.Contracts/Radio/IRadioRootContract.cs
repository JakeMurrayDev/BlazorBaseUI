namespace BlazorBaseUI.Tests.Contracts.Radio;

public interface IRadioRootContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task OverridesBuiltInAttributes();

    // ARIA and role
    Task HasRoleRadio();
    Task HasAriaCheckedFalseByDefault();
    Task HasAriaCheckedTrueWhenChecked();
    Task HasAriaRequiredWhenRequired();
    Task DoesNotHaveAriaRequiredByDefault();
    Task HasTabindexZeroByDefault();
    Task HasTabindexMinusOneWhenDisabled();

    // Disabled
    Task HasDataDisabledWhenDisabled();
    Task DoesNotHaveDataDisabledByDefault();
    Task DoesNotChangeStateWhenClickedDisabled();

    // ReadOnly
    Task HasAriaReadonlyWhenReadOnly();
    Task DoesNotHaveAriaReadonlyByDefault();
    Task DoesNotChangeStateWhenClickedReadOnly();

    // Value prop
    Task DoesNotForwardValuePropToRoot();
    Task AllowsNullValue();

    // Name and hidden input
    Task RendersHiddenRadioInput();
    Task HiddenInputHasCorrectAttributes();
    Task InputHasId();
    Task SetsNameOnInputOnly();
    Task SetsValueOnHiddenInput();

    // Style hooks (data attributes)
    Task HasDataCheckedWhenChecked();
    Task HasDataUncheckedWhenUnchecked();
    Task HasDataDisabledWhenDisabled_StyleHook();
    Task HasDataReadonlyWhenReadOnly_StyleHook();
    Task HasDataRequiredWhenRequired_StyleHook();
    Task PlacesStyleHooksOnRootAndIndicator();
    Task DataCheckedTogglesOnSelection();

    // Context
    Task CascadesContextToChildren();

    // Element reference
    Task ExposesElementReference();

    // RenderAs validation
    Task ThrowsWhenRenderAsDoesNotImplementInterface();

    // ClassValue/StyleValue state
    Task ClassValueReceivesCorrectState();
    Task StyleValueReceivesCorrectState();

    // Group integration
    Task InheritsDisabledFromGroup();
    Task SetsTabindexBasedOnGroupSelection();
}
