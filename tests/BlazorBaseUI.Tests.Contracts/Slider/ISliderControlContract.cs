namespace BlazorBaseUI.Tests.Contracts.Slider;

public interface ISliderControlContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasTouchActionNoneStyle();
    Task HasTabindexMinusOne();
    Task HasDataOrientation();
    Task HasDataDisabledWhenDisabled();
    Task HasDataReadonlyWhenReadOnly();
}
