using BlazorBaseUI.Playwright.Tests.Fixtures;
using BlazorBaseUI.Playwright.Tests.Infrastructure;

namespace BlazorBaseUI.Playwright.Tests.Tests.Collapsible;

public class CollapsibleTestsServer : CollapsibleTestsBase
{
    protected override TestRenderMode RenderMode => TestRenderMode.Server;

    public CollapsibleTestsServer(
        BlazorTestFixture blazorFixture,
        PlaywrightFixture playwrightFixture)
        : base(blazorFixture, playwrightFixture)
    {
    }
}
