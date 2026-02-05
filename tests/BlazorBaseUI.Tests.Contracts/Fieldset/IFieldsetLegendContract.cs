namespace BlazorBaseUI.Tests.Contracts.Fieldset;

public interface IFieldsetLegendContract
{
    Task RendersAsDivByDefault();
    Task SetsAriaLabelledByOnFieldsetAutomatically();
    Task SetsAriaLabelledByWithCustomId();
}
