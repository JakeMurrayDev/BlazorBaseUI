namespace BlazorBaseUI.Tests.Contracts.Accordion;

public interface IAccordionItemContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task HasDataDisabledWhenDisabled();
    Task HasDataDisabledWhenRootDisabled();
    Task HasDataIndexAttribute();
    Task HasDataOrientationAttribute();
}
