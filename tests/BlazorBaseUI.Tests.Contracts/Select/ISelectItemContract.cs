namespace BlazorBaseUI.Tests.Contracts.Select;

public interface ISelectItemContract
{
    Task ShouldSelectItemAndClosePopupWhenClicked();
    Task ShouldNotSelectDisabledItem();
    Task ShouldApplyDataSelectedWhenSelected();
    Task ShouldApplyDataHighlightedWhenHighlighted();
    Task ShouldRenderWithOptionRole();
    Task ShouldSetAriaSelectedTrue();
    Task DisabledItem_HasAriaDisabled();

    // Focus + Disabled
    Task DisabledItem_ShouldStillHighlightOnMouseEnter();

    // Focus on open
    Task ShouldFocusSelectedItemUponOpeningPopup();

    // Quick click guard
    Task QuickSelection_NoSelectOnQuickMouseUpWithPlaceholder();
}
