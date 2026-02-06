namespace BlazorBaseUI.Tests.Contracts.Tabs;

public interface ITabsIndicatorContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomAs();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // ARIA
    Task HasRolePresentation();

    // Visibility
    Task DoesNotRenderWhenValueIsNull();

    // Data attributes
    Task HasDataOrientationHorizontal();
    Task HasDataActivationDirection();

    // Element reference
    Task ExposesElementReference();

    // Validation
    Task ThrowsWhenRenderAsDoesNotImplementInterface();
}
