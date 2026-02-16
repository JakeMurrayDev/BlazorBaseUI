namespace BlazorBaseUI.Tests.Contracts.Slider;

public interface ISliderIndicatorContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasPositioningStyleForSingleValue();
    Task HasPositioningStyleForRangeValue();
    Task HasDataOrientation();
    Task HasDataDisabledWhenDisabled();

    // Vertical orientation tests
    Task HasVerticalPositioningStyleForSingleValue();
    Task HasVerticalPositioningStyleForRangeValue();
}
