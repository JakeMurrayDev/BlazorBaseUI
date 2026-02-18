namespace BlazorBaseUI.Tests.Contracts.Tabs;

public interface ITabsIndicatorContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task RendersChildContent();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // ARIA
    Task HasRolePresentation();

    // Visibility
    Task DoesNotRenderWhenValueIsNull();

    // Data attributes
    Task HasDataActivationDirection();

    // Element reference
    Task ExposesElementReference();
}
