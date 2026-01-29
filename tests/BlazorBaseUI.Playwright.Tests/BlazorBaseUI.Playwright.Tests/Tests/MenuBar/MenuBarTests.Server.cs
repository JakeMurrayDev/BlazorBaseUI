using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.MenuBar;

public class MenuBarTestsServer : MenuBarTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Server;

    public MenuBarTestsServer(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }
}
