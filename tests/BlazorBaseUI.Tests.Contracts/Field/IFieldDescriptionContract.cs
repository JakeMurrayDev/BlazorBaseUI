namespace BlazorBaseUI.Tests.Contracts.Field;

public interface IFieldDescriptionContract
{
    Task RendersAsParagraphByDefault();
    Task SetsAriaDescribedByOnControlAutomatically();
}
