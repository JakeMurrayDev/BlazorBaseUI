namespace BlazorBaseUI.Tests.Contracts.Meter;

public interface IMeterIndicatorContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // Indicator styles
    Task SetsIndicatorStyleForValue();
    Task SetsZeroWidthWhenValueIsZero();
    Task CombinesUserStyleWithIndicatorStyle();
}
