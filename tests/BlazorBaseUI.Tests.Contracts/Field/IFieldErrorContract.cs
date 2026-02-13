namespace BlazorBaseUI.Tests.Contracts.Field;

public interface IFieldErrorContract
{
    Task RendersAsDivWhenInvalid();
    Task RendersWithCustomRender();
    Task SetsAriaDescribedByOnControlAutomatically();
    Task MatchTrueAlwaysRendersErrorMessage();
}
