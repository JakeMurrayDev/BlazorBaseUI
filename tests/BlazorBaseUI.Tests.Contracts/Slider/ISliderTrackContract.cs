namespace BlazorBaseUI.Tests.Contracts.Slider;

public interface ISliderTrackContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasPositionRelativeStyle();
    Task HasDataOrientation();
    Task HasDataDisabledWhenDisabled();
}
