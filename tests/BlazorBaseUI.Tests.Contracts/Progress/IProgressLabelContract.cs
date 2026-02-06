namespace BlazorBaseUI.Tests.Contracts.Progress;

public interface IProgressLabelContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // ID generation
    Task GeneratesAutoId();
    Task UsesProvidedIdFromAdditionalAttributes();

    // Label-root association
    Task NotifiesParentOfLabelId();
    Task CleansUpLabelIdOnDispose();

    // Data attributes
    Task HasDataStatusAttribute();
}
