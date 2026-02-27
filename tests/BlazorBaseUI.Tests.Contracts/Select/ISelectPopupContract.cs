namespace BlazorBaseUI.Tests.Contracts.Select;

public interface ISelectPopupContract
{
    Task HasAriaAttributesWhenNoSelectListPresent();
    Task PlacesAriaAttributesOnSelectListIfPresent();
}
