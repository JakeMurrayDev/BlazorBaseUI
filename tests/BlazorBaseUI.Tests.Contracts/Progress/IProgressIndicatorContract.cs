namespace BlazorBaseUI.Tests.Contracts.Progress;

public interface IProgressIndicatorContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // Indicator styles
    Task SetsIndicatorStyleForDeterminateValue();
    Task SetsZeroWidthWhenValueIsZero();
    Task NoIndicatorStyleForIndeterminateValue();
    Task CombinesUserStyleWithIndicatorStyle();

    // Data attributes
    Task HasDataStatusAttribute();

    // RenderAs validation
    Task ThrowsWhenRenderAsDoesNotImplementInterface();
}
