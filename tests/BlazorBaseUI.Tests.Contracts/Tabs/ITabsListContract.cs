namespace BlazorBaseUI.Tests.Contracts.Tabs;

public interface ITabsListContract
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
    Task HasRoleTablist();
    Task HasAriaOrientationHorizontalByDefault();
    Task HasAriaOrientationVerticalWhenVertical();
    Task CanBeNamedViaAriaLabel();
    Task CanBeNamedViaAriaLabelledby();

    // Data attributes
    Task HasDataOrientationHorizontalByDefault();
    Task HasDataOrientationVerticalWhenVertical();

    // Element reference
    Task ExposesElementReference();

    // Validation
    Task ThrowsWhenRenderAsDoesNotImplementInterface();
    Task ThrowsWhenNotInTabsRoot();
}
