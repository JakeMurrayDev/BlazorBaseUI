namespace BlazorBaseUI.Tests.Contracts.Accordion;

public interface IAccordionHeaderContract
{
    Task RendersAsH3ByDefault();
    Task RendersWithCustomAs();
    Task ForwardsAdditionalAttributes();
    Task AppliesClassValue();
    Task AppliesStyleValue();
    Task HasDataDisabledWhenParentDisabled();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task HasDataIndexAttribute();
    Task HasDataOrientationAttribute();
}
