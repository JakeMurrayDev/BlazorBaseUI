namespace BlazorBaseUI.Tests.Contracts.Field;

public interface IFieldLabelContract
{
    Task RendersAsLabelByDefault();
    Task RendersWithCustomRender();
    Task SetsHtmlForReferencingControlAutomatically();
}
