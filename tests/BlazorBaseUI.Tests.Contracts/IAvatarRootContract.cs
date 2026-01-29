namespace BlazorBaseUI.Tests.Contracts;

public interface IAvatarRootContract
{
    Task RendersAsSpanByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task CombinesClassFromBothSources();
    Task CascadesContextToChildren();
}
