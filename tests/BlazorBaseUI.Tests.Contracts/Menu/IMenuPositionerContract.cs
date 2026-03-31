namespace BlazorBaseUI.Tests.Contracts.Menu;

public interface IMenuPositionerContract
{
    Task DefaultsAlignToCenter();
    Task NestedMenuDefaultsAlignToStart();
    Task ExplicitAlignOverridesNestedDefault();
}
