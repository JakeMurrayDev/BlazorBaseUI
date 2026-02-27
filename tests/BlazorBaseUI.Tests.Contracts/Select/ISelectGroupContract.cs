namespace BlazorBaseUI.Tests.Contracts.Select;

public interface ISelectGroupContract
{
    Task ShouldRenderGroupWithLabel();
    Task ShouldAssociateLabelWithGroup();
}
