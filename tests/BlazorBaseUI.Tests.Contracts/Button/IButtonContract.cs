namespace BlazorBaseUI.Tests.Contracts.Button;

public interface IButtonContract
{
    // Rendering
    Task RendersAsButtonByDefault();
    Task RendersWithCustomAs();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // Native button attributes
    Task NativeButton_HasTypeButton();
    Task NativeButton_HasDisabledAttributeWhenDisabled();
    Task NativeButton_DoesNotHaveAriaDisabledWhenDisabled();
    Task NativeButton_DoesNotHaveRoleButton();

    // Non-native button attributes
    Task NonNativeButton_HasRoleButton();
    Task NonNativeButton_DoesNotHaveTypeButton();
    Task NonNativeButton_HasAriaDisabledTrueWhenDisabled();
    Task NonNativeButton_HasTabIndexMinusOneWhenDisabled();

    // Data attributes
    Task HasDataDisabledWhenDisabled();
    Task DoesNotHaveDataDisabledWhenNotDisabled();

    // FocusableWhenDisabled - native
    Task NativeFocusableWhenDisabled_DoesNotHaveDisabledAttribute();
    Task NativeFocusableWhenDisabled_HasAriaDisabledTrue();
    Task NativeFocusableWhenDisabled_HasTabIndex();
    Task NativeFocusableWhenDisabled_HasDataDisabled();

    // FocusableWhenDisabled - non-native
    Task NonNativeFocusableWhenDisabled_HasAriaDisabledTrue();
    Task NonNativeFocusableWhenDisabled_HasTabIndex();
    Task NonNativeFocusableWhenDisabled_HasRoleButton();
    Task NonNativeFocusableWhenDisabled_HasDataDisabled();

    // TabIndex
    Task NativeButton_HasDefaultTabIndex();
    Task NonNativeButton_HasDefaultTabIndex();
    Task ForwardsExplicitTabIndex();
    Task NonNativeDisabled_HasTabIndexMinusOne();

    // State cascading
    Task CascadesButtonStateToClassValue();
    Task CascadesButtonStateDisabledTrue();

    // Element reference
    Task ExposesElementReference();

    // RenderAs validation
    Task ThrowsWhenRenderAsDoesNotImplementInterface();
}
