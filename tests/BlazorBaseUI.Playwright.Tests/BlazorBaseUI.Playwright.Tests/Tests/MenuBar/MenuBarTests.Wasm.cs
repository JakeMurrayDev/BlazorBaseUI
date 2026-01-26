using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.MenuBar;

public class MenuBarTestsWasm : MenuBarTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Wasm;

    public MenuBarTestsWasm(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }
}
