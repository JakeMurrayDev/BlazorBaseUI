namespace BlazorBaseUI.Tests.Contracts.NavigationMenu;

public interface INavigationMenuPortalContract
{
    Task RendersWhenMounted();
    Task DoesNotRenderWhenNotMounted();
    Task RendersWhenKeepMounted();
}
