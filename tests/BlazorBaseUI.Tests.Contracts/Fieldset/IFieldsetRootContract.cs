namespace BlazorBaseUI.Tests.Contracts.Fieldset;

public interface IFieldsetRootContract
{
    Task RendersAsFieldsetByDefault();
    Task RendersWithCustomRender();
}
