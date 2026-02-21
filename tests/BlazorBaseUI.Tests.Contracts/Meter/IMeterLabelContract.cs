namespace BlazorBaseUI.Tests.Contracts.Meter;

public interface IMeterLabelContract
{
    // Rendering
    Task RendersAsSpanByDefault();
    Task RendersWithCustomRender();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();

    // ID generation
    Task GeneratesAutoId();
    Task UsesProvidedIdFromAdditionalAttributes();

    // Label-root association
    Task NotifiesParentOfLabelId();
    Task CleansUpLabelIdOnDispose();
}
