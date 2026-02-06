namespace BlazorBaseUI.Tests.Contracts.Progress;

public interface IProgressTrackContract
{
    // Rendering
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // Data attributes
    Task HasDataStatusAttribute();

    // RenderAs validation
    Task ThrowsWhenRenderAsDoesNotImplementInterface();
}
