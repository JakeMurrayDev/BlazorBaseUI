namespace BlazorBaseUI.Tests.Contracts.Form;

public interface IFormContract
{
    Task RendersAsFormByDefault();
    Task SetsNoValidateByDefault();
    Task NoValidateIsAlwaysPresent();
    Task MarksControlInvalidWhenErrorsProvided();
    Task DoesNotMarkControlInvalidWhenNoErrors();
}
