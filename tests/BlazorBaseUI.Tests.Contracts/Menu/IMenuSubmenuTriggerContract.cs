namespace BlazorBaseUI.Tests.Contracts.Menu;

public interface IMenuSubmenuTriggerContract
{
    Task RendersAsDivByDefault();
    Task RendersWithCustomAs();
    Task HasAriaHaspopupMenu();
    Task HasAriaExpandedFalseWhenClosed();
    Task HasAriaExpandedTrueWhenOpen();
    Task HasDataOpenWhenOpen();
    Task HasDataClosedWhenClosed();
    Task HasDataDisabledWhenDisabled();
    Task RequiresSubmenuContext();
}
