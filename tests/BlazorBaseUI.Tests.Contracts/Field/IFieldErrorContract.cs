namespace BlazorBaseUI.Tests.Contracts.Field;

public interface IFieldErrorContract
{
    Task RendersAsDivWhenInvalid();
    Task SetsAriaDescribedByOnControlAutomatically();
    Task MatchTrueAlwaysRendersErrorMessage();
}
