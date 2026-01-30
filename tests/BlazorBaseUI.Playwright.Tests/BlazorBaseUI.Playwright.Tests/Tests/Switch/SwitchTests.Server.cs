using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Switch;

public class SwitchTestsServer : SwitchTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Server;

    public SwitchTestsServer(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }
}
