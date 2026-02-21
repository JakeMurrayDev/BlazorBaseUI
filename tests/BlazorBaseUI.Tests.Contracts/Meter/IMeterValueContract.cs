namespace BlazorBaseUI.Tests.Contracts.Meter;

public interface IMeterValueContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // ARIA
    Task HasAriaHidden();

    // Content rendering
    Task RendersFormattedValueWhenNoChildContent();
    Task RendersCustomFormattedValue();
    Task ChildContentReceivesFormattedValueAndNumber();
}
