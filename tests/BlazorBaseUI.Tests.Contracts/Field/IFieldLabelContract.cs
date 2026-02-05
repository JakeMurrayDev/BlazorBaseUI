namespace BlazorBaseUI.Tests.Contracts.Field;

public interface IFieldLabelContract
{
    Task RendersAsLabelByDefault();
    Task SetsHtmlForReferencingControlAutomatically();
}
