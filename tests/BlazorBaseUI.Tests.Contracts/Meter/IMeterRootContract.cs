namespace BlazorBaseUI.Tests.Contracts.Meter;

public interface IMeterRootContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();

    // ARIA attributes
    Task HasRoleMeter();
    Task SetsAriaValueMin();
    Task SetsAriaValueMax();
    Task SetsAriaValueNow();
    Task SetsAriaValueText();
    Task SetsAriaLabelledByWhenLabelPresent();
    Task UpdatesAriaValueNowWhenValueChanges();

    // Formatting
    Task FormatsValueWithCustomFormat();
    Task FormatsValueWithFormatProvider();
    Task GetAriaValueTextCallbackOverridesDefault();
    Task AriaValueTextUsesFormattedValueWhenFormatProvided();

    // Context cascading
    Task CascadesContextToChildren();

    // Element reference
    Task ExposesElementReference();
}
