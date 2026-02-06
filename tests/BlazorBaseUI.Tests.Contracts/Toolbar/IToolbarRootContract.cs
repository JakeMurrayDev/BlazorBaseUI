namespace BlazorBaseUI.Tests.Contracts.Toolbar;

public interface IToolbarRootContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // ARIA
    Task HasRoleToolbar();
    Task HasAriaOrientationHorizontalByDefault();
    Task HasAriaOrientationVerticalWhenVertical();

    // Data attributes
    Task HasDataOrientationHorizontalByDefault();
    Task HasDataOrientationVerticalWhenVertical();
    Task HasDataDisabledWhenDisabled();
    Task DoesNotHaveDataDisabledWhenNotDisabled();

    // State cascading
    Task ClassValueReceivesToolbarRootState();
    Task ClassValueReceivesDisabledTrue();
    Task ClassValueReceivesOrientationVertical();

    // Element reference
    Task ExposesElementReference();

    // Validation
    Task ThrowsWhenRenderAsDoesNotImplementInterface();

    // Context cascading
    Task CascadesDisabledToButton();
    Task CascadesOrientationToButton();
}
