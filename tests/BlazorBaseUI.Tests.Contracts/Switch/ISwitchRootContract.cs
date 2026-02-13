namespace BlazorBaseUI.Tests.Contracts.Switch;

public interface ISwitchRootContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task OverridesBuiltInAttributes();

    // ARIA and role
    Task HasRoleSwitch();
    Task HasAriaCheckedFalseByDefault();
    Task HasAriaCheckedTrueWhenChecked();
    Task HasTabindexZeroByDefault();
    Task HasTabindexMinusOneWhenDisabled();

    // Disabled
    Task UsesAriaDisabledInsteadOfHtmlDisabled();
    Task DoesNotHaveDisabledAttributeByDefault();

    // ReadOnly
    Task HasAriaReadonlyWhenReadOnly();
    Task DoesNotHaveAriaReadonlyByDefault();

    // Required
    Task HasAriaRequiredWhenRequired();
    Task DoesNotHaveAriaRequiredByDefault();

    // Name and Value on hidden input
    Task SetsNameOnInputOnly();
    Task DoesNotSetValueByDefault();
    Task SetsValueOnInputOnly();

    // Hidden checkbox input
    Task RendersHiddenCheckboxInput();
    Task HiddenInputHasCorrectAttributes();
    Task InputHasIdWhenNotNativeButton();

    // UncheckedValue
    Task RendersUncheckedValueHiddenInput();
    Task DoesNotRenderUncheckedValueWhenChecked();

    // Style hooks (data attributes)
    Task HasDataCheckedWhenChecked();
    Task HasDataUncheckedWhenUnchecked();
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadonlyWhenReadOnly();
    Task HasDataRequiredWhenRequired();

    // Controlled/Uncontrolled
    Task UncontrolledModeUsesDefaultChecked();
    Task ControlledModeRespectsCheckedParameter();
    Task UpdatesStateWhenControlledValueChanges();

    // Event callbacks
    Task InvokesOnCheckedChangeOnInputChange();
    Task InvokesCheckedChangedOnInputChange();
    Task DoesNotInvokeCallbacksWhenDisabled();
    Task DoesNotInvokeCallbacksWhenReadOnly();
    Task OnCheckedChangeCancellationPreventsStateChange();

    // Context cascading
    Task CascadesContextToChildren();

    // NativeButton mode
    Task NativeButtonRendersAsButton();
    Task NativeButtonHasRoleSwitch();
    Task NativeButtonHasDisabledAttribute();
    Task NativeButtonHasCorrectId();

    // Element reference
    Task ExposesElementReference();
    Task ExposesInputElementReference();

    // ClassValue/StyleValue state
    Task ClassValueReceivesCorrectState();
    Task StyleValueReceivesCorrectState();
}
